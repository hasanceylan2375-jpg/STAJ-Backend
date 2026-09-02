using STAJ.Data;
using STAJ.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace STAJ.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        public AuthService(AppDbContext context, IConfiguration configuration) { _context = context; _configuration = configuration; }
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
            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre)) return "Kullanıcı adı ve şifre zorunludur.";
            if (kullaniciAdi.Length < 3) return "Kullanıcı adı en az 3 karakter olmalıdır.";
            if (sifre.Length < 4) return "Şifre en az 4 karakter olmalıdır.";
            if (_context.Kullanicilar.Any(x => x.KullaniciAdi == kullaniciAdi)) return "Bu kullanıcı adı zaten kullanılıyor.";
            _context.Kullanicilar.Add(new Kullanici { KullaniciAdi = kullaniciAdi, Sifre = BCrypt.Net.BCrypt.HashPassword(sifre), Rol = "User" });
            _context.SaveChanges();
            return null;
        }
        public string TokenOlustur(Kullanici kullanici)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, kullanici.KullaniciAdi), new Claim(ClaimTypes.Role, kullanici.Rol) };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(issuer:_configuration["Jwt:Issuer"], audience:_configuration["Jwt:Audience"], claims:claims, expires:DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes",15)), signingCredentials:credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<RefreshToken> RefreshTokenOlusturAsync(Kullanici kullanici)
        {
            var refreshToken = new RefreshToken { UserId=kullanici.Id, Token=Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), CreatedAt=DateTime.UtcNow, ExpiresAt=DateTime.UtcNow.AddDays(_configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays",7)) };
            _context.RefreshTokens.Add(refreshToken); await _context.SaveChangesAsync(); return refreshToken;
        }
        public async Task<(string AccessToken, RefreshToken RefreshToken)?> RefreshAsync(string refreshTokenValue)
        {
            var refreshToken = await _context.RefreshTokens.Include(x=>x.User).FirstOrDefaultAsync(x=>x.Token==refreshTokenValue);
            if(refreshToken==null||refreshToken.RevokedAt.HasValue||refreshToken.ExpiresAt<=DateTime.UtcNow)return null;
            refreshToken.RevokedAt=DateTime.UtcNow; var newRefreshToken=await RefreshTokenOlusturAsync(refreshToken.User); refreshToken.ReplacedByToken=newRefreshToken.Token; var accessToken=TokenOlustur(refreshToken.User); await _context.SaveChangesAsync(); return(accessToken,newRefreshToken);
        }
        public async Task<bool> LogoutAsync(string refreshTokenValue)
        {
            var refreshToken=await _context.RefreshTokens.FirstOrDefaultAsync(x=>x.Token==refreshTokenValue); if(refreshToken==null||refreshToken.RevokedAt.HasValue)return false; refreshToken.RevokedAt=DateTime.UtcNow; await _context.SaveChangesAsync(); return true;
        }
        public void KullaniciOlustur(string kullaniciAdi,string sifre,string rol){_context.Kullanicilar.Add(new Kullanici{KullaniciAdi=kullaniciAdi,Sifre=BCrypt.Net.BCrypt.HashPassword(sifre),Rol=rol});_context.SaveChanges();}
    }
}
