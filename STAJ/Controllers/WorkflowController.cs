using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAJ.Data;
using STAJ.DTOs;
using STAJ.Entities;
using STAJ.Services;

namespace STAJ.Controllers;

[ApiController]
[Route("api/workflows")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private const string CustomerCreate = "CustomerCreate";
    private const string Pending = "Pending";
    private const string Approved = "Approved";
    private const string Rejected = "Rejected";

    private readonly AppDbContext _context;
    private readonly IMusteriService _musteriService;
    private readonly IValidator<Musteri> _validator;

    public WorkflowController(AppDbContext context, IMusteriService musteriService, IValidator<Musteri> validator)
    {
        _context = context;
        _musteriService = musteriService;
        _validator = validator;
    }

    [HttpPost("start")]
    public async Task<ActionResult<WorkflowResponse>> Start(WorkflowStartRequest request)
    {
        var validation = await _validator.ValidateAsync(request.Musteri);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(x => x.ErrorMessage));

        if (_musteriService.TcKimlikNoVarMi(request.Musteri.TcKimlikNo ?? string.Empty))
            return Conflict("Bu T.C. Kimlik No ile kayıtlı bir müşteri zaten var.");

        var workflow = new WorkflowRequest
        {
            Type = CustomerCreate,
            Status = Pending,
            RequestedBy = User.Identity?.Name ?? "unknown",
            DataJson = JsonSerializer.Serialize(request.Musteri),
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<WorkflowRequest>().Add(workflow);
        await _context.SaveChangesAsync();
        return Ok(ToResponse(workflow));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowResponse>>> Mine()
    {
        var user = User.Identity?.Name ?? "unknown";
        var items = await _context.Set<WorkflowRequest>()
            .Where(x => x.RequestedBy == user)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return Ok(items.Select(ToResponse));
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<WorkflowResponse>>> PendingRequests()
    {
        var items = await _context.Set<WorkflowRequest>()
            .Where(x => x.Type == CustomerCreate && x.Status == Pending)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
        return Ok(items.Select(ToResponse));
    }

    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WorkflowResponse>> Approve(int id, WorkflowDecisionRequest request)
    {
        var workflow = await _context.Set<WorkflowRequest>().FirstOrDefaultAsync(x => x.Id == id);
        if (workflow == null) return NotFound("Workflow talebi bulunamadı.");
        if (workflow.Status != Pending) return Conflict("Bu workflow talebi artık onay beklemiyor.");

        var musteri = JsonSerializer.Deserialize<Musteri>(workflow.DataJson);
        if (musteri == null) return BadRequest("Workflow verisi okunamadı.");

        var validation = await _validator.ValidateAsync(musteri);
        if (!validation.IsValid) return BadRequest(validation.Errors.Select(x => x.ErrorMessage));
        if (_musteriService.TcKimlikNoVarMi(musteri.TcKimlikNo ?? string.Empty))
            return Conflict("Onay sırasında aynı T.C. Kimlik No ile müşteri bulundu.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _musteriService.Ekle(musteri);
            workflow.Status = Approved;
            workflow.ReviewedAt = DateTime.UtcNow;
            workflow.ReviewedBy = User.Identity?.Name ?? "manager";
            workflow.ReviewComment = request.Comment?.Trim();
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok(ToResponse(workflow, musteri));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<WorkflowResponse>> Reject(int id, WorkflowDecisionRequest request)
    {
        var workflow = await _context.Set<WorkflowRequest>().FirstOrDefaultAsync(x => x.Id == id);
        if (workflow == null) return NotFound("Workflow talebi bulunamadı.");
        if (workflow.Status != Pending) return Conflict("Bu workflow talebi artık onay beklemiyor.");

        workflow.Status = Rejected;
        workflow.ReviewedAt = DateTime.UtcNow;
        workflow.ReviewedBy = User.Identity?.Name ?? "manager";
        workflow.ReviewComment = request.Comment?.Trim();
        await _context.SaveChangesAsync();
        return Ok(ToResponse(workflow));
    }

    private static WorkflowResponse ToResponse(WorkflowRequest workflow, Musteri? musteri = null)
    {
        musteri ??= JsonSerializer.Deserialize<Musteri>(workflow.DataJson);
        return new WorkflowResponse(workflow.Id, workflow.Type, workflow.Status, workflow.RequestedBy, musteri, workflow.CreatedAt, workflow.ReviewedAt, workflow.ReviewedBy, workflow.ReviewComment);
    }
}
