using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace STAJ.Services;

public sealed record ExtractedPage(string Text, int? PageNumber, string? Section);

public sealed class RagTextExtractor
{
    public async Task<IReadOnlyList<ExtractedPage>> ExtractAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenReadStream();
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => ExtractPdf(stream),
            ".docx" => ExtractDocx(stream),
            ".txt" => new[] { new ExtractedPage(await ReadTextAsync(stream, cancellationToken), null, null) },
            _ => throw new InvalidOperationException("Desteklenmeyen dosya formatı.")
        };
    }

    private static IReadOnlyList<ExtractedPage> ExtractPdf(Stream stream)
    {
        using var document = PdfDocument.Open(stream);
        return document.GetPages()
            .Select(page => new ExtractedPage(page.Text?.Trim() ?? string.Empty, page.Number, null))
            .Where(page => !string.IsNullOrWhiteSpace(page.Text))
            .ToList();
    }

    private static IReadOnlyList<ExtractedPage> ExtractDocx(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
            return Array.Empty<ExtractedPage>();

        var text = string.Join("\n", body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>()
            .Select(x => x.Text))
            .Trim();

        return string.IsNullOrWhiteSpace(text)
            ? Array.Empty<ExtractedPage>()
            : new[] { new ExtractedPage(text, null, null) };
    }

    private static async Task<string> ReadTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return (await reader.ReadToEndAsync(cancellationToken)).Trim();
    }
}
