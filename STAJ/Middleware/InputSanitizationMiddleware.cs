namespace STAJ.Middleware
{
    /// <summary>
    /// Rejects malformed/control-character input at the HTTP boundary.
    /// Validation is performed early; output encoding remains the responsibility
    /// of the serializer/UI according to the target output context.
    /// </summary>
    public class InputSanitizationMiddleware
    {
        private const int MaxPathLength = 2048;
        private const int MaxQueryValueLength = 2048;
        private const int MaxHeaderValueLength = 8192;

        private readonly RequestDelegate _next;

        public InputSanitizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Value is { Length: > MaxPathLength })
            {
                context.Response.StatusCode = StatusCodes.Status414UriTooLong;
                await context.Response.WriteAsJsonAsync(new
                {
                    mesaj = "İstek yolu çok uzun."
                });
                return;
            }

            foreach (var queryParameter in context.Request.Query)
            {
                if (queryParameter.Key.Length > MaxQueryValueLength ||
                    queryParameter.Value.Any(value => value.Length > MaxQueryValueLength || ContainsControlCharacter(value)))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        mesaj = "İstek parametreleri geçersiz."
                    });
                    return;
                }
            }

            foreach (var header in context.Request.Headers)
            {
                if (header.Key.Length > MaxHeaderValueLength ||
                    header.Value.Any(value => value.Length > MaxHeaderValueLength || ContainsHeaderControlCharacter(value)))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        mesaj = "İstek başlıkları geçersiz."
                    });
                    return;
                }
            }

            await _next(context);
        }

        private static bool ContainsControlCharacter(string value)
        {
            foreach (var character in value)
            {
                if (character == '\0' || char.IsControl(character) && character != '\t')
                    return true;
            }

            return false;
        }

        private static bool ContainsHeaderControlCharacter(string value)
        {
            foreach (var character in value)
            {
                if (character == '\0' || character == '\r' || character == '\n' || char.IsControl(character))
                    return true;
            }

            return false;
        }
    }
}
