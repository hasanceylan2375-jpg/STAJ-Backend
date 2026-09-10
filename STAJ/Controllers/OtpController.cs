using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using STAJ.Models;
using STAJ.Services;

namespace STAJ.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("auth")]
public class OtpController : ControllerBase
{
    private readonly OtpService _otpService;
    private readonly MailService _mailService;

    public OtpController(OtpService otpService, MailService mailService)
    {
        _otpService = otpService;
        _mailService = mailService;
    }

    [HttpPost("gonder")]
    public async Task<IActionResult> Gonder([FromBody] OtpRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = _otpService.Olustur(request.Hedef, request.Kanal, request.KullaniciId, ip);
        if (!result.Success) return BadRequest(new { mesaj = result.Message });

        try
        {
            await _mailService.SendMailAsync(
                request.Hedef,
                "STAJ OTP Doğrulama Kodu",
                $"<h2>OTP Doğrulama</h2><p>Doğrulama kodunuz:</p><h1 style='letter-spacing:8px'>{result.Code}</h1><p>Bu kod 60 saniye geçerlidir ve tek kullanımlıktır.</p>");
            return Ok(new { mesaj = "OTP başarıyla e-posta adresinize gönderildi.", kanal = request.Kanal, gecerlilikSuresi = 60 });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mesaj = "OTP oluşturuldu ancak e-posta gönderilemedi. SMTP ayarlarını kontrol edin." });
        }
    }

    [HttpPost("dogrula")]
    public IActionResult Dogrula([FromBody] OtpDogrulaRequest request)
    {
        var result = _otpService.Dogrula(request.Hedef, request.Kod);
        if (!result.Success) return BadRequest(new { mesaj = result.Message });
        return Ok(new { mesaj = result.Message, dogrulandi = true });
    }
}
