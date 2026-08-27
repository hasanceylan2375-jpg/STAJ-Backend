namespace STAJ.Results
{
    public class DataResult<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public DataResult(bool success, string message, T? data = default)
        {
            Success = success;
            Message = message;
            Data = data;
        }
    }
}
