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
                                ? context.User.Identity?.Name
                                : null,
                            HttpMethod = context.Request.Method,
                            Path = context.Request.Path.Value ?? string.Empty,
                            StatusCode = context.Response.StatusCode,
                            IpAddress = context.Connection.RemoteIpAddress?.ToString(),
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
    }
}
