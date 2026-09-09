namespace STAJ.Services;

public sealed record RagTextChunk(string Text, int ChunkIndex, int? PageNumber, string? Section, int TokenEstimate);

public sealed class RagChunker
{
    private const int TargetTokens = 750;
    private const int OverlapTokens = 120;

    public IReadOnlyList<RagTextChunk> Chunk(IReadOnlyList<ExtractedPage> pages)
    {
        var chunks = new List<RagTextChunk>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var words = page.Text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                continue;

            var start = 0;
            while (start < words.Length)
            {
                var length = Math.Min(TargetTokens, words.Length - start);
                var text = string.Join(' ', words.Skip(start).Take(length)).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    chunks.Add(new RagTextChunk(text, chunkIndex++, page.PageNumber, page.Section, length));
                }

                if (start + length >= words.Length)
                    break;

                start += Math.Max(1, length - OverlapTokens);
            }
        }

        return chunks;
    }
}
