using STAJ.Data;
using STAJ.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
namespace STAJ.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public Kullanici? Login(string kullaniciAdi, string sifre)
        {
            var kullanici = _context.Kullanicilar
                .FirstOrDefault(x => x.KullaniciAdi == kullaniciAdi);

            if (kullanici == null)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(sifre, kullanici.Sifre))
                return null;

            return kullanici;
        }
        public string TokenOlustur(Kullanici kullanici)
        {
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, kullanici.KullaniciAdi),
        new Claim(ClaimTypes.Role, kullanici.Rol)
    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("STAJ-Projesi-Super-Gizli-JWT-Anahtari-2026")
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            var token = new JwtSecurityToken(
                issuer: "STAJ",
                audience: "STAJFrontend",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public void KullaniciOlustur(string kullaniciAdi, string sifre, string rol)
        {
            var hashliSifre = BCrypt.Net.BCrypt.HashPassword(sifre);

            var kullanici = new Kullanici
            {
                KullaniciAdi = kullaniciAdi,
                Sifre = hashliSifre,
                Rol = rol
            };

            _context.Kullanicilar.Add(kullanici);
            _context.SaveChanges();
        }
    }
}