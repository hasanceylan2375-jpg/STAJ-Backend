using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAJ.Entities;
using STAJ.Services;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class MailController : ControllerBase
    {
        private readonly MailService _mailService;
        private readonly IConfiguration _configuration;
        private static readonly CircuitBreakerService CircuitBreaker = new();

        public MailController(MailService mailService, IConfiguration configuration)
        {
            _mailService = mailService;
            _configuration = configuration;
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.To) ||
                string.IsNullOrWhiteSpace(request.Subject) ||
                string.IsNullOrWhiteSpace(request.Body))
            {
                return BadRequest("Alıcı, konu ve mesaj alanları boş bırakılamaz.");
            }

            if (CircuitBreaker.IsOpen)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mesaj = "Mail servisi geçici olarak kullanılamıyor. Lütfen daha sonra tekrar deneyin." });

            var failureThreshold = Math.Max(1, _configuration.GetValue<int>("Performance:CircuitBreakerFailureThreshold", 3));
            var breakSeconds = Math.Max(1, _configuration.GetValue<int>("Performance:CircuitBreakerBreakSeconds", 30));

            try
            {
                await _mailService.SendMailAsync(request.To, request.Subject, request.Body);
                CircuitBreaker.RecordSuccess();
                return Ok(new { mesaj = "Mail başarıyla gönderildi." });
            }
            catch
            {
                CircuitBreaker.RecordFailure(failureThreshold, TimeSpan.FromSeconds(breakSeconds));
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mesaj = "Mail servisi şu anda kullanılamıyor. Lütfen daha sonra tekrar deneyin." });
            }
        }
    }
}