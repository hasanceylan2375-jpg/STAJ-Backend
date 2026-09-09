using ClosedXML.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using STAJ.Data;
using STAJ.Entities;
using STAJ.Events;
using STAJ.Results;
using STAJ.Resources;
using STAJ.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MusteriController : ControllerBase
    {
        private const long MaxDosyaBoyutu = 5 * 1024 * 1024;
        private static readonly string[] IzinliUzantilar = [".jpg", ".jpeg", ".png", ".webp"];
        private readonly IMusteriService _service; private readonly IStringLocalizer<SharedResource> _localizer; private readonly IValidator<Musteri> _validator; private readonly AppDbContext _context; private readonly IDomainEventDispatcher _eventDispatcher;
        public MusteriController(IMusteriService service, IStringLocalizer<SharedResource> localizer, IValidator<Musteri> validator, AppDbContext context, IDomainEventDispatcher eventDispatcher) { _service = service; _localizer = localizer; _validator = validator; _context = context; _eventDispatcher = eventDispatcher; }
        [HttpPost("fotoğraf")]
        [EnableRateLimiting("write")]
        public async Task<IActionResult> FotografYukle(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Fotoğraf seçilmedi."); if (file.Length > MaxDosyaBoyutu) return BadRequest("Dosya boyutu en fazla 5 MB olabilir."); var uzanti = Path.GetExtension(file.FileName).ToLowerInvariant(); if (!IzinliUzantilar.Contains(uzanti)) return BadRequest("Sadece JPG, JPEG, PNG veya WEBP dosyaları yüklenebilir.");
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"); Directory.CreateDirectory(uploadsPath); await using var yuklenenDosyaStream = file.OpenReadStream(); var yuklenenHash = Convert.ToHexString(await SHA256.HashDataAsync(yuklenenDosyaStream)); foreach (var mevcutDosya in Directory.GetFiles(uploadsPath)) { await using var mevcutDosyaStream = System.IO.File.OpenRead(mevcutDosya); if (yuklenenHash == Convert.ToHexString(await SHA256.HashDataAsync(mevcutDosyaStream))) return BadRequest("Bu fotoğraf daha önce yüklenmiş."); }
            var dosyaAdi = $"{Guid.NewGuid()}{uzanti}"; var dosyaYolu = Path.Combine(uploadsPath, dosyaAdi); await using var stream = new FileStream(dosyaYolu, FileMode.Create); await file.CopyToAsync(stream); return Ok(new { url = $"/uploads/{dosyaAdi}" });
        }
        [HttpGet("fotoğraf/indir/{dosyaAdi}")][EnableRateLimiting("read")]
        public IActionResult FotografIndir(string dosyaAdi) { var guvenliDosyaAdi = Path.GetFileName(dosyaAdi); var dosyaYolu = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", guvenliDosyaAdi); if (!System.IO.File.Exists(dosyaYolu)) return NotFound("Fotoğraf bulunamadı."); var provider = new FileExtensionContentTypeProvider(); if (!provider.TryGetContentType(dosyaYolu, out var contentType)) contentType = "application/octet-stream"; return PhysicalFile(dosyaYolu, contentType, guvenliDosyaAdi); }
        [HttpGet][EnableRateLimiting("read")]
        public IActionResult Getir([FromQuery] string? search = null, [FromQuery] string? sort = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 5) => Ok(new DataResult<object>(true, _localizer["CustomersRetrieved"], _service.Getir(search, sort, page, pageSize)));
        [HttpGet("excel")][Authorize(Policy = "AdminOnly")][EnableRateLimiting("read")]
        public IActionResult ExcelAktar([FromQuery] string? search = null, [FromQuery] string? sort = null) { var musteriler = _service.Getir(search, sort, 1, 10000); using var workbook = new XLWorkbook(); var worksheet = workbook.Worksheets.Add("Müşteriler"); var headers = new[] { "ID", "Ad", "Soyad", "Telefon", "E-posta" }; for (var i = 0; i < headers.Length; i++) worksheet.Cell(1, i + 1).Value = headers[i]; for (var i = 0; i < musteriler.Count; i++) { var m = musteriler[i]; var row = i + 2; worksheet.Cell(row, 1).Value = m.Id; worksheet.Cell(row, 2).Value = m.Ad; worksheet.Cell(row, 3).Value = m.Soyad; worksheet.Cell(row, 4).Value = m.Telefon; worksheet.Cell(row, 5).Value = m.Email; } worksheet.Range(1, 1, 1, headers.Length).Style.Font.Bold = true; worksheet.Columns().AdjustToContents(); using var stream = new MemoryStream(); workbook.SaveAs(stream); return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "musteriler.xlsx"); }
        [HttpGet("cursor")][EnableRateLimiting("read")]
        public IActionResult CursorIleGetir([FromQuery] int? lastId = null, [FromQuery] int pageSize = 5) { var musteriler = _service.CursorIleGetir(lastId, pageSize); var nextCursor = musteriler.Count > 0 ? musteriler.Last().Id : (int?)null; return Ok(new DataResult<object>(true, _localizer["CustomersRetrieved"], new { items = musteriler, nextCursor })); }
        [HttpGet("{id}")][EnableRateLimiting("read")]
        public IActionResult IdyeGoreGetir(int id) { var musteri = _service.IdyeGoreGetir(id); return musteri == null ? NotFound(new DataResult<object>(false, _localizer["CustomerNotFound"])) : Ok(new DataResult<Musteri>(true, _localizer["CustomerRetrieved"], musteri)); }
        [HttpPost][Authorize(Roles = "Admin")][EnableRateLimiting("write")]
        public async Task<IActionResult> Ekle(Musteri musteri, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey) { if (string.IsNullOrWhiteSpace(idempotencyKey)) return BadRequest(new DataResult<object>(false, "Idempotency-Key zorunludur.")); var requestHash = HashRequest(musteri); var existingRecord = await _context.IdempotencyRecords.FirstOrDefaultAsync(x => x.Key == idempotencyKey); if (existingRecord != null) { if (existingRecord.ExpireAt <= DateTime.UtcNow) _context.IdempotencyRecords.Remove(existingRecord); else { if (existingRecord.RequestHash != requestHash) return Conflict(new DataResult<object>(false, "Bu Idempotency-Key farklı bir istek için kullanılamaz.")); return StatusCode(existingRecord.StatusCode, JsonSerializer.Deserialize<object>(existingRecord.Response!)); } } var validationResult = _validator.Validate(musteri); if (!validationResult.IsValid) return ValidationError(validationResult.Errors.Select(x => x.ErrorMessage)); if (_service.TcKimlikNoVarMi(musteri.TcKimlikNo!)) return BadRequest(new DataResult<object>(false, "Bu T.C. Kimlik No ile kayıtlı bir müşteri zaten var.")); await using var transaction = await _context.Database.BeginTransactionAsync(); try { _service.Ekle(musteri); var response = new DataResult<Musteri>(true, _localizer["CustomerAdded"], musteri); _context.IdempotencyRecords.Add(new IdempotencyRecord { Key = idempotencyKey, RequestHash = requestHash, Response = JsonSerializer.Serialize(response), StatusCode = StatusCodes.Status200OK, ExpireAt = DateTime.UtcNow.AddHours(24) }); await _context.SaveChangesAsync(); await transaction.CommitAsync(); await _eventDispatcher.PublishAsync(new MusteriEklendiEvent(musteri)); return Ok(response); } catch { await transaction.RollbackAsync(); throw; } }
        [HttpPut("{id}")][Authorize(Roles = "Admin")][EnableRateLimiting("write")]
        public async Task<IActionResult> Guncelle(int id, Musteri musteri) { var validationResult = _validator.Validate(musteri); if (!validationResult.IsValid) return ValidationError(validationResult.Errors.Select(x => x.ErrorMessage)); if (_service.TcKimlikNoVarMi(musteri.TcKimlikNo!, id)) return BadRequest(new DataResult<object>(false, "Bu T.C. Kimlik No başka bir müşteriye ait.")); var mevcutMusteri = _service.IdyeGoreGetir(id); if (mevcutMusteri == null) return NotFound(new DataResult<object>(false, _localizer["CustomerToUpdateNotFound"])); mevcutMusteri.Ad = musteri.Ad; mevcutMusteri.Soyad = musteri.Soyad; mevcutMusteri.Telefon = musteri.Telefon; mevcutMusteri.Email = musteri.Email; mevcutMusteri.TcKimlikNo = musteri.TcKimlikNo; mevcutMusteri.DogumTarihi = musteri.DogumTarihi; mevcutMusteri.ProfilFotoUrl = musteri.ProfilFotoUrl; _service.Guncelle(mevcutMusteri); await _eventDispatcher.PublishAsync(new MusteriGuncellendiEvent(mevcutMusteri)); return Ok(new DataResult<Musteri>(true, _localizer["CustomerUpdated"], mevcutMusteri)); }
        [HttpDelete("{id}")][Authorize(Roles = "Admin")][EnableRateLimiting("write")]
        public async Task<IActionResult> Sil(int id) { var mevcutMusteri = _service.IdyeGoreGetir(id); if (mevcutMusteri == null) return NotFound(new DataResult<object>(false, _localizer["CustomerToDeleteNotFound"])); _service.Sil(id); await _eventDispatcher.PublishAsync(new MusteriSilindiEvent(id, mevcutMusteri.Ad, mevcutMusteri.Soyad)); return Ok(new DataResult<object>(true, _localizer["CustomerDeleted"])); }
        private static string HashRequest(Musteri musteri) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(musteri))));
        private BadRequestObjectResult ValidationError(IEnumerable<string> errors) => BadRequest(new DataResult<List<string>>(false, "Gönderilen bilgiler geçersiz.", errors.ToList()));
    }
}