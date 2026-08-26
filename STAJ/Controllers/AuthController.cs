using Microsoft.AspNetCore.Mvc;
using STAJ.Services;
using STAJ.Entities;
namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
{
    var kullanici = _authService.Login(request.KullaniciAdi, request.Sifre);

            if (kullanici == null)
            {
                return Unauthorized("Kullanıcı adı veya şifre hatalı.");
            }
            var token = _authService.TokenOlustur(kullanici);
            return Ok(new
            {
                mesaj = "Giriş başarılı.",
                kullaniciAdi = kullanici.KullaniciAdi,
                rol = kullanici.Rol,
                accessToken = token
            });
        }

    }
}