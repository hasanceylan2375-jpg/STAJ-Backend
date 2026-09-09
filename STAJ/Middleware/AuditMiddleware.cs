using System.Diagnostics;
using STAJ.Data;
using STAJ.Entities;

namespace STAJ.Middleware
{
    public class AuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditMiddleware> _logger;

        public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                if (!context.Request.Path.StartsWithSegments("/swagger"))
                {
                    try
                    {
                        var auditLog = new AuditLog
                        {
                            KullaniciAdi = context.User.Identity?.IsAuthenticated == true
                                ? SanitizeForAudit(context.User.Identity?.Name, 256)
                                : null,
                            HttpMethod = SanitizeForAudit(context.Request.Method, 16),
                            Path = SanitizeForAudit(context.Request.Path.Value, 2048),
                            StatusCode = context.Response.StatusCode,
                            IpAddress = SanitizeForAudit(context.Connection.RemoteIpAddress?.ToString(), 64),
                            DurationMs = stopwatch.ElapsedMilliseconds
                        };

                        dbContext.AuditLogs.Add(auditLog);
                        await dbContext.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Audit kaydı oluşturulamadı.");
                    }
                }
            }
        }

        private static string? SanitizeForAudit(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var sanitized = new string(value
                .Where(character => !char.IsControl(character))
                .ToArray())
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

            return sanitized.Length <= maxLength ? sanitized : sanitized[..maxLength];
        }
    }
}
