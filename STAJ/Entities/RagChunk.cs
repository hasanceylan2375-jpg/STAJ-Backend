using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace STAJ.Entities;

public class RagChunk
{
    public int Id { get; set; }
    public int RagDocumentId { get; set; }
    public RagDocument RagDocument { get; set; } = null!;
    public int ChunkIndex { get; set; }
    public string Text { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public string? Section { get; set; }
    public int TokenEstimate { get; set; }

    [Column(TypeName = "vector(1536)")]
    public Vector Embedding { get; set; } = new Vector(new float[1536]);
}
