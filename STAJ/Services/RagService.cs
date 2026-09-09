using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using STAJ.Data;
using STAJ.Entities;

namespace STAJ.Services;

public sealed class RagService
{
    private const long MaxFileSize = 10 * 1024 * 1024;
    private const int MaxCompanyNameLength = 100;
    private const int MaxQuestionLength = 2000;
    private const int TopK = 5;

    private readonly AppDbContext _db;
    private readonly RagTextExtractor _extractor;
    private readonly RagChunker _chunker;
    private readonly RagOpenAiService _ai;
    private readonly ILogger<RagService> _logger;

    public RagService(
        AppDbContext db,
        RagTextExtractor extractor,
        RagChunker chunker,
        RagOpenAiService ai,
        ILogger<RagService> logger)
    {
        _db = db;
        _extractor = extractor;
        _chunker = chunker;
        _ai = ai;
        _logger = logger;
    }

    public async Task<(int DocumentId, int ChunkCount)> UploadAsync(
        string companyName,
        IFormFile file,
        string uploadedBy,
        CancellationToken cancellationToken)
    {
        ValidateCompany(companyName);
        ValidateFile(file);
        await EnsureVectorSchemaAsync(cancellationToken);

        var pages = await _extractor.ExtractAsync(file, cancellationToken);
        var chunks = _chunker.Chunk(pages);
        if (chunks.Count == 0)
            throw new InvalidOperationException("Dosyadan okunabilir metin çıkarılamadı.");
        if (chunks.Count > 5000)
            throw new InvalidOperationException("Doküman çok büyük. En fazla 5000 chunk kabul edilir.");

        var embeddings = await _ai.CreateEmbeddingsAsync(chunks.Select(x => x.Text).ToList(), cancellationToken);
        if (embeddings.Count != chunks.Count || embeddings.Any(x => x.ToArray().Length != 1536))
            throw new InvalidOperationException("Embedding boyutu RAG yapılandırmasıyla eşleşmiyor. text-embedding-3-small ve 1536 boyut bekleniyor.");

        var document = new RagDocument
        {
            CompanyName = companyName.Trim(),
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType ?? "application/octet-stream",
            UploadedBy = uploadedBy.Length > 100 ? uploadedBy[..100] : uploadedBy,
            UploadedAtUtc = DateTime.UtcNow
        };

        for (var i = 0; i < chunks.Count; i++)
        {
            document.Chunks.Add(new RagChunk
            {
                ChunkIndex = chunks[i].ChunkIndex,
                Text = chunks[i].Text,
                PageNumber = chunks[i].PageNumber,
                Section = chunks[i].Section,
                TokenEstimate = chunks[i].TokenEstimate,
                Embedding = embeddings[i]
            });
        }

        _db.RagDocuments.Add(document);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("RAG dokümanı indekslendi. DocumentId={DocumentId}, ChunkCount={ChunkCount}, Company={Company}", document.Id, document.Chunks.Count, document.CompanyName);
        return (document.Id, document.Chunks.Count);
    }

    public async Task<RagAskResult> AskAsync(string companyName, string question, CancellationToken cancellationToken)
    {
        ValidateCompany(companyName);
        if (string.IsNullOrWhiteSpace(question) || question.Length > MaxQuestionLength)
            throw new ArgumentException("Soru boş olamaz ve en fazla 2000 karakter olabilir.");

        await EnsureVectorSchemaAsync(cancellationToken);
        var queryEmbedding = (await _ai.CreateEmbeddingsAsync(new[] { question.Trim() }, cancellationToken)).Single();

        var retrieved = await _db.RagChunks
            .AsNoTracking()
            .Where(x => x.RagDocument.CompanyName == companyName.Trim())
            .OrderBy(x => x.Embedding.CosineDistance(queryEmbedding))
            .Take(TopK)
            .Select(x => new
            {
                x.Id,
                x.Text,
                x.PageNumber,
                x.Embedding,
                CompanyName = x.RagDocument.CompanyName,
                FileName = x.RagDocument.FileName
            })
            .ToListAsync(cancellationToken);

        if (retrieved.Count == 0)
        {
            return new RagAskResult(
                "Bu şirket için yüklenmiş bir kural/doküman bulunamadı.",
                Array.Empty<RagSourceResult>());
        }

        var chunks = retrieved.Select(x => new RagRetrievedChunk(
            x.CompanyName,
            x.FileName,
            x.Text,
            x.PageNumber,
            x.Embedding.CosineDistance(queryEmbedding),
            x.Id)).ToList();

        var answer = await _ai.GenerateAnswerAsync(question.Trim(), chunks, cancellationToken);
        var sources = chunks.Select(x => new RagSourceResult(
            x.ChunkId,
            x.CompanyName,
            x.FileName,
            x.PageNumber,
            Math.Max(0, 1 - x.Distance))).ToList();

        return new RagAskResult(answer, sources);
    }

    private async Task EnsureVectorSchemaAsync(CancellationToken cancellationToken)
    {
        await _db.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS vector;", cancellationToken);
        await _db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""RagDocuments"" (
                ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                ""CompanyName"" text NOT NULL,
                ""FileName"" text NOT NULL,
                ""ContentType"" text NOT NULL,
                ""UploadedBy"" text NOT NULL,
                ""UploadedAtUtc"" timestamp with time zone NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ""RagChunks"" (
                ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                ""RagDocumentId"" integer NOT NULL REFERENCES ""RagDocuments""(""Id"") ON DELETE CASCADE,
                ""ChunkIndex"" integer NOT NULL,
                ""Text"" text NOT NULL,
                ""PageNumber"" integer NULL,
                ""Section"" text NULL,
                ""TokenEstimate"" integer NOT NULL,
                ""Embedding"" vector(1536) NOT NULL,
                CONSTRAINT ""UX_RagChunks_Document_Chunk"" UNIQUE (""RagDocumentId"", ""ChunkIndex"")
            );
            CREATE INDEX IF NOT EXISTS ""IX_RagDocuments_CompanyName_UploadedAtUtc"" ON ""RagDocuments"" (""CompanyName"", ""UploadedAtUtc"");
            CREATE INDEX IF NOT EXISTS ""IX_RagChunks_Embedding_Hnsw"" ON ""RagChunks"" USING hnsw (""Embedding"" vector_cosine_ops);
        ", cancellationToken);
    }

    private static void ValidateCompany(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName) || companyName.Trim().Length > MaxCompanyNameLength)
            throw new ArgumentException("Şirket adı boş olamaz ve en fazla 100 karakter olabilir.");
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file is null || file.Length <= 0 || file.Length > MaxFileSize)
            throw new ArgumentException("Dosya boş olamaz ve en fazla 10 MB olabilir.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".pdf", ".docx", ".txt" };
        if (!allowed.Contains(extension, StringComparer.Ordinal))
            throw new ArgumentException("Sadece PDF, DOCX ve TXT dosyaları kabul edilir.");

        using var stream = file.OpenReadStream();
        Span<byte> header = stackalloc byte[4];
        var read = stream.Read(header);

        var valid = extension switch
        {
            ".pdf" => read >= 4 && header.SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            ".docx" => read >= 2 && header[0] == 0x50 && header[1] == 0x4B,
            ".txt" => true,
            _ => false
        };

        if (!valid)
            throw new ArgumentException("Dosya içeriği uzantıyla eşleşmiyor.");
    }
}
