namespace STAJ.Exceptions;

public sealed class ValidationException : Exception
{
    public ValidationException(
        string messageKey,
        IReadOnlyDictionary<string, string[]> validationErrors)
        : base(messageKey)
    {
        MessageKey = messageKey;
        ValidationErrors = validationErrors;
    }

    public string MessageKey { get; }
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string messageKey, string message)
        : base(message)
    {
        MessageKey = messageKey;
    }

    public string MessageKey { get; }
}

public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string messageKey, string message)
        : base(message)
    {
        MessageKey = messageKey;
    }

    public string MessageKey { get; }
}

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string messageKey = "FORBIDDEN")
        : base("Bu işlem için yetkiniz yok.")
    {
        MessageKey = messageKey;
    }

    public string MessageKey { get; }
}

public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string messageKey = "UNAUTHORIZED")
        : base("Kimlik doğrulaması gerekli veya oturum geçersiz.")
    {
        MessageKey = messageKey;
    }

    public string MessageKey { get; }
}
