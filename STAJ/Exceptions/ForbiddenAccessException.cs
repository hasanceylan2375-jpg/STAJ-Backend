namespace STAJ.Exceptions
{
    public class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException(string message = "Bu işlem için yetkiniz bulunmuyor.") : base(message) { }
    }
}
