namespace STAJ.Entities;

public class WorkflowRequest
{
    public int Id { get; set; }
    public string Type { get; set; } = "CustomerCreate";
    public string Status { get; set; } = "Draft";
    public string RequestedBy { get; set; } = string.Empty;
    public string DataJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewComment { get; set; }
}
