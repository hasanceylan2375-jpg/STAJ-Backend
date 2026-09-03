namespace STAJ.Exceptions
{
    public class UnauthorizedException : Exception
    {
        public UnauthorizedException(string message = "Bu işlem için giriş yapmanız gerekiyor.") : base(message) { }
    }
}
