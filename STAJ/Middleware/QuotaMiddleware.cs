using STAJ.Services;

namespace STAJ.Middleware
{
    public sealed class QuotaMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly QuotaService _quotaService;

        public QuotaMiddleware(RequestDelegate next, IConfiguration configuration, QuotaService quotaService)
        {
            _next = next;
            _configuration = configuration;
            _quotaService = quotaService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var dailyLimit = Math.Max(1, _configuration.GetValue<int>("Quota:DailyLimit", 10000));
            var monthlyLimit = Math.Max(dailyLimit, _configuration.GetValue<int>("Quota:MonthlyLimit", 100000));
            var key = context.User.Identity?.IsAuthenticated == true
                ? $"user:{context.User.Identity.Name ?? context.User.FindFirst("sub")?.Value ?? "authenticated"}"
                : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

            if (!_quotaService.TryConsume(key, dailyLimit, monthlyLimit, out var dailyRemaining, out var monthlyRemaining))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["X-Quota-Daily-Remaining"] = dailyRemaining.ToString();
                context.Response.Headers["X-Quota-Monthly-Remaining"] = monthlyRemaining.ToString();
                await context.Response.WriteAsJsonAsync(new { mesaj = "Kullanım kotanız doldu. Lütfen daha sonra tekrar deneyin." });
                return;
            }

            context.Response.Headers["X-Quota-Daily-Remaining"] = dailyRemaining.ToString();
            context.Response.Headers["X-Quota-Monthly-Remaining"] = monthlyRemaining.ToString();
            await _next(context);
        }
    }
}
