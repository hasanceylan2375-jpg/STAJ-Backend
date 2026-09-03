namespace STAJ.Results;

public sealed class ApiErrorResponse
{
    public ApiErrorResponse(
        int statusCode,
        string messageKey,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        StatusCode = statusCode;
        MessageKey = messageKey;
        Message = message;
        ValidationErrors = validationErrors;
    }

    public bool Success => false;
    public int StatusCode { get; }
    public string MessageKey { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }
}
