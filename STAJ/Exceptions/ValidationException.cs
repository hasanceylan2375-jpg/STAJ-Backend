namespace STAJ.Exceptions
{
    public class ValidationException : Exception
    {
        public Dictionary<string, string[]>? ValidationErrors { get; }

        public ValidationException(string message) : base(message) { }

        public ValidationException(string message, Dictionary<string, string[]> validationErrors) : base(message)
        {
            ValidationErrors = validationErrors;
        }
    }
}
