using System.Text.RegularExpressions;
using Serilog.Context;

namespace STAJ.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";
        private static readonly Regex SafeCorrelationId = new("^[A-Za-z0-9._-]{1,64}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestedCorrelationId = context.Request.Headers[HeaderName].FirstOrDefault();
            var correlationId = !string.IsNullOrWhiteSpace(requestedCorrelationId) && SafeCorrelationId.IsMatch(requestedCorrelationId)
                ? requestedCorrelationId
                : Guid.NewGuid().ToString("N");

            context.Response.Headers[HeaderName] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("RequestPath", SanitizeForLog(context.Request.Path.Value)))
            using (LogContext.PushProperty("RequestMethod", SanitizeForLog(context.Request.Method)))
            {
                await _next(context);
            }
        }

        private static string SanitizeForLog(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var sanitized = new string(value
                .Where(character => !char.IsControl(character) || character == '\t')
                .ToArray())
                .Replace('\r', ' ')
                .Replace('\n', ' ');

            return sanitized.Length <= 2048 ? sanitized : sanitized[..2048];
        }
    }
}
