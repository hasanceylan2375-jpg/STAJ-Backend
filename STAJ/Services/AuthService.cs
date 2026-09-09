using STAJ.Data;
using STAJ.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace STAJ.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public Kullanici? Login(string kullaniciAdi, string sifre)
        {
            var kullanici = _context.Kullanicilar.FirstOrDefault(x => x.KullaniciAdi == kullaniciAdi);
            if (kullanici == null || !BCrypt.Net.BCrypt.Verify(sifre, kullanici.Sifre)) return null;
            return kullanici;
        }

        public string? KayitOl(string kullaniciAdi, string sifre)
        {
            kullaniciAdi = kullaniciAdi?.Trim() ?? string.Empty;
            sifre ??= string.Empty;

            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
                return "Kullanıcı adı ve şifre zorunludur.";
            if (kullaniciAdi.Length < 3 || kullaniciAdi.Length > 50)
                return "Kullanıcı adı 3-50 karakter arasında olmalıdır.";

            var passwordError = ValidatePassword(sifre);
            if (passwordError != null) return passwordError;

            if (_context.Kullanicilar.Any(x => x.KullaniciAdi == kullaniciAdi))
                return "Bu kullanıcı adı zaten kullanılıyor.";

            _context.Kullanicilar.Add(new Kullanici
            {
                KullaniciAdi = kullaniciAdi,
                Sifre = BCrypt.Net.BCrypt.HashPassword(sifre, BCrypt.Net.BCrypt.GenerateSalt(12)),
                Rol = "User"
            });
            _context.SaveChanges();
            return null;
        }

        public string TokenOlustur(Kullanici kullanici)
        {
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
                throw new InvalidOperationException("JWT anahtarı güvenli şekilde yapılandırılmamış.");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
                new Claim(ClaimTypes.Role, kullanici.Rol)
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 15)),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> RefreshTokenOlusturAsync(Kullanici kullanici)
        {
            var refreshToken = new RefreshToken
            {
                UserId = kullanici.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7))
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task<(string AccessToken, RefreshToken RefreshToken)?> RefreshAsync(string refreshTokenValue)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenValue)) return null;

            var refreshToken = await _context.RefreshTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == refreshTokenValue);

            if (refreshToken == null || refreshToken.RevokedAt.HasValue || refreshToken.ExpiresAt <= DateTime.UtcNow)
                return null;

            refreshToken.RevokedAt = DateTime.UtcNow;
            var newRefreshToken = await RefreshTokenOlusturAsync(refreshToken.User);
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            var accessToken = TokenOlustur(refreshToken.User);
            await _context.SaveChangesAsync();
            return (accessToken, newRefreshToken);
        }

        public async Task<bool> LogoutAsync(string refreshTokenValue)
        {
            if (string.IsNullOrWhiteSpace(refreshTokenValue)) return false;

            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshTokenValue);
            if (refreshToken == null || refreshToken.RevokedAt.HasValue) return false;

            refreshToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public void KullaniciOlustur(string kullaniciAdi, string sifre, string rol)
        {
            var passwordError = ValidatePassword(sifre);
            if (passwordError != null) throw new ArgumentException(passwordError);

            _context.Kullanicilar.Add(new Kullanici
            {
                KullaniciAdi = kullaniciAdi.Trim(),
                Sifre = BCrypt.Net.BCrypt.HashPassword(sifre, BCrypt.Net.BCrypt.GenerateSalt(12)),
                Rol = rol
            });
            _context.SaveChanges();
        }

        private static string? ValidatePassword(string password)
        {
            if (password.Length < 8) return "Şifre en az 8 karakter olmalıdır.";
            if (password.Length > 128) return "Şifre en fazla 128 karakter olabilir.";
            if (!Regex.IsMatch(password, "[A-Z]")) return "Şifre en az bir büyük harf içermelidir.";
            if (!Regex.IsMatch(password, "[a-z]")) return "Şifre en az bir küçük harf içermelidir.";
            if (!Regex.IsMatch(password, "[0-9]")) return "Şifre en az bir rakam içermelidir.";
            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]")) return "Şifre en az bir özel karakter içermelidir.";
            return null;
        }
    }
}
