using Microsoft.AspNetCore.Mvc;
using STAJ.Entities;
using STAJ.Services;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MailController : ControllerBase
    {
        private readonly MailService _mailService;

        public MailController(MailService mailService)
        {
            _mailService = mailService;
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

            await _mailService.SendMailAsync(request.To, request.Subject, request.Body);

            return Ok(new { mesaj = "Mail başarıyla gönderildi." });
        }
    }
}