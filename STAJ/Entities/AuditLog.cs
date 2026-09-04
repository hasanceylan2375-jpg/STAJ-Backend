namespace STAJ.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? KullaniciAdi { get; set; }
        public string HttpMethod { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? IpAddress { get; set; }
        public long DurationMs { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
