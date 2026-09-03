using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using STAJ.Entities;
using STAJ.Exceptions;
using STAJ.Services;

namespace STAJ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        var hata = _authService.KayitOl(request.KullaniciAdi, request.Sifre);

        if (hata is not null)
        {
            throw new BusinessRuleException("REGISTRATION_FAILED", hata);
        }

        return Ok(new { mesaj = "Kayıt başarılı. Giriş yapabilirsiniz." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var kullanici = _authService.Login(request.KullaniciAdi, request.Sifre);

        if (kullanici is null)
        {
            throw new UnauthorizedException("INVALID_CREDENTIALS");
        }

        var accessToken = _authService.TokenOlustur(kullanici);
        var refreshToken = await _authService.RefreshTokenOlusturAsync(kullanici);

        return Ok(new
        {
            mesaj = "Giriş başarılı.",
            kullaniciAdi = kullanici.KullaniciAdi,
            rol = kullanici.Rol,
            accessToken,
            refreshToken = refreshToken.Token,
            refreshTokenExpiresAt = refreshToken.ExpiresAt
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshAsync(request.RefreshToken);

        if (result is null)
        {
            throw new UnauthorizedException("INVALID_REFRESH_TOKEN");
        }

        return Ok(new
        {
            accessToken = result.Value.AccessToken,
            refreshToken = result.Value.RefreshToken.Token,
            refreshTokenExpiresAt = result.Value.RefreshToken.ExpiresAt
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken);

        if (!result)
        {
            throw new BusinessRuleException(
                "INVALID_REFRESH_TOKEN",
                "Refresh token geçersiz veya zaten çıkış yapılmış.");
        }

        return Ok(new { mesaj = "Çıkış başarılı." });
    }
}
