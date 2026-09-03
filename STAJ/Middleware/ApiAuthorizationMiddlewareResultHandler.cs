using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using STAJ.Results;

namespace STAJ.Middleware;

public sealed class ApiAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            return WriteErrorAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "UNAUTHORIZED",
                "Kimlik doğrulaması gerekli veya oturum geçersiz.");
        }

        if (authorizeResult.Forbidden)
        {
            return WriteErrorAsync(
                context,
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                "Bu işlem için yetkiniz yok.");
        }

        return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string messageKey,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(
            new ApiErrorResponse(statusCode, messageKey, message));
    }
}
