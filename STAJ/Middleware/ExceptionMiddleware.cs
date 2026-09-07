using STAJ.Exceptions;
using STAJ.Results;

namespace STAJ.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault()
                    ?? context.TraceIdentifier;

                _logger.LogError(
                    ex,
                    "İstek işlenirken hata oluştu. CorrelationId: {CorrelationId}, Method: {Method}, Path: {Path}",
                    correlationId,
                    context.Request.Method,
                    context.Request.Path);

                await HandleExceptionAsync(context, ex, correlationId);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string correlationId)
        {
            var (statusCode, message, errors) = exception switch
            {
                ValidationException ex => (StatusCodes.Status400BadRequest, ex.Message, ex.ValidationErrors),
                UnauthorizedException ex => (StatusCodes.Status401Unauthorized, ex.Message, (Dictionary<string, string[]>?)null),
                ForbiddenAccessException ex => (StatusCodes.Status403Forbidden, ex.Message, (Dictionary<string, string[]>?)null),
                NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message, (Dictionary<string, string[]>?)null),
                BusinessRuleException ex => (StatusCodes.Status400BadRequest, ex.Message, (Dictionary<string, string[]>?)null),
                _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu.", (Dictionary<string, string[]>?)null)
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            var response = new DataResult<Dictionary<string, string[]>?>(false, message, errors);
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
