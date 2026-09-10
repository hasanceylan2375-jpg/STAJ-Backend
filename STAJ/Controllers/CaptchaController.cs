using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using STAJ.Services;

namespace STAJ.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaptchaController : ControllerBase
{
    private readonly CaptchaService _captchaService;

    public CaptchaController(CaptchaService captchaService)
    {
        _captchaService = captchaService;
    }

    [HttpGet]
    [EnableRateLimiting("auth")]
    public ActionResult<CaptchaChallenge> Get()
    {
        return Ok(_captchaService.Create());
    }
}
