using Microsoft.AspNetCore.Http;
using STAJ.Exceptions;
using STAJ.Results;

namespace STAJ.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
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
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (statusCode, response) = CreateResponse(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Beklenmeyen bir hata oluştu.");
            }
            else
            {
                _logger.LogWarning(exception, "İstemci kaynaklı bir API hatası oluştu.");
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(response);
        }
    }

    private static (int StatusCode, ApiErrorResponse Response) CreateResponse(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    validationException.MessageKey,
                    "Gönderilen bilgiler geçersiz.",
                    validationException.ValidationErrors)),

            BusinessRuleException businessRuleException => (
                StatusCodes.Status400BadRequest,
                new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    businessRuleException.MessageKey,
                    businessRuleException.Message)),

            UnauthorizedException unauthorizedException => (
                StatusCodes.Status401Unauthorized,
                new ApiErrorResponse(
                    StatusCodes.Status401Unauthorized,
                    unauthorizedException.MessageKey,
                    unauthorizedException.Message)),

            ForbiddenAccessException forbiddenAccessException => (
                StatusCodes.Status403Forbidden,
                new ApiErrorResponse(
                    StatusCodes.Status403Forbidden,
                    forbiddenAccessException.MessageKey,
                    forbiddenAccessException.Message)),

            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                new ApiErrorResponse(
                    StatusCodes.Status404NotFound,
                    notFoundException.MessageKey,
                    notFoundException.Message)),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    StatusCodes.Status500InternalServerError,
                    "INTERNAL_SERVER_ERROR",
                    "Beklenmeyen bir hata oluştu."))
        };
    }
}
