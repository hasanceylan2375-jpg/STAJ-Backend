namespace STAJ.Entities
{
    public class IdempotencyRecord
    {
        public int Id { get; set; }
        public string Key { get; set; } = null!;
        public string RequestHash { get; set; } = null!;
        public string? Response { get; set; }
        public int StatusCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpireAt { get; set; }
    }
}
