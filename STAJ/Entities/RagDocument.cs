namespace STAJ.Entities;

public class RagDocument
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public ICollection<RagChunk> Chunks { get; set; } = new List<RagChunk>();
}
