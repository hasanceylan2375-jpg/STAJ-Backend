namespace STAJ.Services;

public sealed record RagRetrievedChunk(
    string CompanyName,
    string FileName,
    string Text,
    int? PageNumber,
    double Distance,
    int ChunkId);

public sealed record RagAskResult(
    string Answer,
    IReadOnlyList<RagSourceResult> Sources);

public sealed record RagSourceResult(
    int ChunkId,
    string CompanyName,
    string FileName,
    int? PageNumber,
    double Similarity);
