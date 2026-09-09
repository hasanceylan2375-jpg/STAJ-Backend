using STAJ.Entities;

namespace STAJ.DTOs;

public record WorkflowStartRequest(Musteri Musteri);
public record WorkflowDecisionRequest(string? Comment);
public record WorkflowResponse(int Id, string Type, string Status, string RequestedBy, Musteri? Musteri, DateTime CreatedAt, DateTime? ReviewedAt, string? ReviewedBy, string? ReviewComment);
