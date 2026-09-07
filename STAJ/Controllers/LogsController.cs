using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAJ.Data;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class LogsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<LogsController> _logger;

        public LogsController(AppDbContext dbContext, ILogger<LogsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet("recent")]
        public async Task<IActionResult> Recent([FromQuery] int limit = 50)
        {
            limit = Math.Clamp(limit, 1, 200);

            var logs = await _dbContext.AuditLogs
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Take(limit)
                .Select(x => new
                {
                    x.Id,
                    x.KullaniciAdi,
                    x.HttpMethod,
                    x.Path,
                    x.StatusCode,
                    x.IpAddress,
                    x.DurationMs,
                    x.CreatedAt
                })
                .ToListAsync();

            _logger.LogInformation("Log dashboard için {Count} kayıt getirildi.", logs.Count);
            return Ok(logs);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var now = DateTime.UtcNow;
            var start = now.AddHours(-24);

            var query = _dbContext.AuditLogs.AsNoTracking().Where(x => x.CreatedAt >= start);
            var total = await query.CountAsync();
            var errors = await query.CountAsync(x => x.StatusCode >= 500);
            var clientErrors = await query.CountAsync(x => x.StatusCode >= 400 && x.StatusCode < 500);
            var successful = await query.CountAsync(x => x.StatusCode >= 200 && x.StatusCode < 400);
            var averageDuration = total == 0 ? 0 : await query.AverageAsync(x => (double)x.DurationMs);

            return Ok(new
            {
                period = "Son 24 saat",
                total,
                successful,
                clientErrors,
                errors,
                averageDurationMs = Math.Round(averageDuration, 2)
            });
        }
    }
}
