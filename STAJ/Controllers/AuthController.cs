using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using STAJ.Services;
using STAJ.Entities;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        public AuthController(AuthService authService) { _authService = authService; }

        [HttpPost("register")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var hata = await _authService.KayitOlAsync(request.KullaniciAdi, request.Sifre);
            if (hata != null) return BadRequest(new { mesaj = hata });
            return Ok(new { mesaj = "Kayıt başarılı. Giriş yapabilirsiniz." });
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var kullanici = _authService.Login(request.KullaniciAdi, request.Sifre);
            if (kullanici == null) return Unauthorized("Kullanıcı adı veya şifre hatalı.");
            var accessToken = _authService.TokenOlustur(kullanici);
            var refreshToken = await _authService.RefreshTokenOlusturAsync(kullanici);
            return Ok(new { mesaj = "Giriş başarılı.", kullaniciAdi = kullanici.KullaniciAdi, rol = kullanici.Rol, accessToken, refreshToken = refreshToken.Token, refreshTokenExpiresAt = refreshToken.ExpiresAt });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshAsync(request.RefreshToken);
            if (result == null) return Unauthorized("Refresh token geçersiz veya süresi dolmuş.");
            return Ok(new { accessToken = result.Value.AccessToken, refreshToken = result.Value.RefreshToken.Token, refreshTokenExpiresAt = result.Value.RefreshToken.ExpiresAt });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.LogoutAsync(request.RefreshToken);
            if (!result) return BadRequest("Refresh token geçersiz veya zaten çıkış yapılmış.");
            return Ok(new { mesaj = "Çıkış başarılı." });
        }
    }
}
