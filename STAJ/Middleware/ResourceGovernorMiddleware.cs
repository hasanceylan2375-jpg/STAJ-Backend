namespace STAJ.Middleware
{
    /// <summary>
    /// Lightweight application-level resource guard for memory and request size.
    /// CPU/DB pool limits remain infrastructure/provider concerns; this guard prevents
    /// oversized requests from consuming excessive application memory.
    /// </summary>
    public sealed class ResourceGovernorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public ResourceGovernorMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var maxRequestBytes = Math.Max(1L, _configuration.GetValue<long>("Performance:MaxRequestBodyBytes", 10 * 1024 * 1024));
            var maxManagedMemoryBytes = Math.Max(1L, _configuration.GetValue<long>("Performance:MaxManagedMemoryBytes", 512L * 1024 * 1024));

            if (context.Request.ContentLength > maxRequestBytes)
            {
                context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                await context.Response.WriteAsJsonAsync(new { mesaj = "İstek gövdesi izin verilen boyutu aşıyor." });
                return;
            }

            var memoryInfo = GC.GetGCMemoryInfo();
            if (memoryInfo.HeapSizeBytes > maxManagedMemoryBytes)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers["Retry-After"] = "5";
                await context.Response.WriteAsJsonAsync(new { mesaj = "Sunucu kaynakları yoğun. Lütfen daha sonra tekrar deneyin." });
                return;
            }

            await _next(context);
        }
    }
}
