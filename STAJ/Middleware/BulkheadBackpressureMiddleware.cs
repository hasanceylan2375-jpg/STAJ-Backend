using System.Collections.Concurrent;

namespace STAJ.Middleware
{
    /// <summary>
    /// Concurrency limiting: caps total active requests.
    /// Bulkhead: isolates concurrent work per user/IP.
    /// Backpressure: waits briefly for capacity instead of immediately rejecting every request.
    /// </summary>
    public sealed class BulkheadBackpressureMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _partitions = new();
        private readonly SemaphoreSlim _globalSemaphore;

        public BulkheadBackpressureMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
            var globalLimit = Math.Max(1, configuration.GetValue<int>("Performance:MaxConcurrentRequests", 50));
            _globalSemaphore = new SemaphoreSlim(globalLimit, globalLimit);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var perUserLimit = Math.Max(1, _configuration.GetValue<int>("Performance:BulkheadPerUserConcurrency", 5));
            var waitMilliseconds = Math.Max(0, _configuration.GetValue<int>("Performance:BackpressureWaitMilliseconds", 2000));
            var key = context.User.Identity?.IsAuthenticated == true
                ? $"user:{context.User.Identity.Name ?? context.User.FindFirst("sub")?.Value ?? "authenticated"}"
                : $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

            var partition = _partitions.GetOrAdd(key, _ => new SemaphoreSlim(perUserLimit, perUserLimit));
            var globalAcquired = false;
            var partitionAcquired = false;

            try
            {
                globalAcquired = await _globalSemaphore.WaitAsync(waitMilliseconds, context.RequestAborted);
                if (!globalAcquired)
                {
                    await RejectBusyAsync(context);
                    return;
                }

                partitionAcquired = await partition.WaitAsync(waitMilliseconds, context.RequestAborted);
                if (!partitionAcquired)
                {
                    await RejectBusyAsync(context);
                    return;
                }

                await _next(context);
            }
            finally
            {
                if (partitionAcquired)
                    partition.Release();
                if (globalAcquired)
                    _globalSemaphore.Release();
            }
        }

        private static async Task RejectBusyAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers["Retry-After"] = "2";
            await context.Response.WriteAsJsonAsync(new
            {
                mesaj = "Sunucu yoğun. İsteğiniz için kapasite bekleniyor, lütfen tekrar deneyin."
            });
        }
    }
}
