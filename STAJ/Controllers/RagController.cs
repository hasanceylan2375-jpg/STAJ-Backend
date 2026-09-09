using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAJ.Services;

namespace STAJ.Controllers;

[ApiController]
[Route("api/rag")]
[Authorize]
public sealed class RagController : ControllerBase
{
    private readonly RagService _ragService;

    public RagController(RagService ragService)
    {
        _ragService = ragService;
    }

    [HttpPost("documents/upload")]
    [Authorize(Policy = "AdminOnly")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] string companyName,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var uploadedBy = User.Identity?.Name ?? User.FindFirst("sub")?.Value ?? "unknown";
            var result = await _ragService.UploadAsync(companyName, file, uploadedBy, cancellationToken);
            return Ok(new
            {
                mesaj = "Doküman başarıyla işlendi ve vektör veritabanına aktarıldı.",
                documentId = result.DocumentId,
                chunkCount = result.ChunkCount
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mesaj = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { mesaj = ex.Message });
        }
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask(
        [FromBody] RagAskRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _ragService.AskAsync(request.CompanyName, request.Question, cancellationToken);
            return Ok(new
            {
                cevap = result.Answer,
                kaynaklar = result.Sources
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mesaj = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mesaj = "RAG servisi şu anda kullanılamıyor." });
        }
    }
}

public sealed class RagAskRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
}
