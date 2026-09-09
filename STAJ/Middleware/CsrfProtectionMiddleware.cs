namespace STAJ.Middleware
{
    public sealed class CsrfProtectionMiddleware
    {
        private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "HEAD", "OPTIONS"
        };

        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public CsrfProtectionMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!SafeMethods.Contains(context.Request.Method))
            {
                var origin = context.Request.Headers.Origin.ToString();
                if (!string.IsNullOrWhiteSpace(origin))
                {
                    var allowedOrigins = _configuration.GetSection("Security:AllowedOrigins").Get<string[]>()
                        ?? ["http://localhost:4200"];

                    if (!allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { success = false, message = "İstek kaynağı güvenlik politikası tarafından reddedildi." });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
