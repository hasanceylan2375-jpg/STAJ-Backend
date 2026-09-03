using ClosedXML.Excel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Localization;
using STAJ.Entities;
using STAJ.Exceptions;
using ApiValidationException = STAJ.Exceptions.ValidationException;
using STAJ.Results;
using STAJ.Resources;
using STAJ.Services;
using System.Security.Cryptography;

namespace STAJ.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MusteriController : ControllerBase
{
    private readonly MusteriService _service;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IValidator<Musteri> _validator;

    public MusteriController(
        MusteriService service,
        IStringLocalizer<SharedResource> localizer,
        IValidator<Musteri> validator)
    {
        _service = service;
        _localizer = localizer;
        _validator = validator;
    }

    [HttpPost("fotoğraf")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("write")]
    public async Task<IActionResult> FotografYukle(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw ValidationError("file", "Fotoğraf seçilmedi.");
        }

        const long maxDosyaBoyutu = 5 * 1024 * 1024;
        if (file.Length > maxDosyaBoyutu)
        {
            throw ValidationError("file", "Dosya boyutu en fazla 5 MB olabilir.");
        }

        var uzantilar = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var uzanti = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!uzantilar.Contains(uzanti))
        {
            throw ValidationError("file", "Sadece JPG, JPEG, PNG veya WEBP dosyaları yüklenebilir.");
        }

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);

        await using var yuklenenDosyaStream = file.OpenReadStream();
        var yuklenenHash = Convert.ToHexString(
            await SHA256.HashDataAsync(yuklenenDosyaStream));

        foreach (var mevcutDosya in Directory.GetFiles(uploadsPath))
        {
            await using var mevcutDosyaStream = System.IO.File.OpenRead(mevcutDosya);
            var mevcutHash = Convert.ToHexString(
                await SHA256.HashDataAsync(mevcutDosyaStream));

            if (yuklenenHash == mevcutHash)
            {
                throw new BusinessRuleException(
                    "DUPLICATE_PROFILE_PHOTO",
                    "Bu fotoğraf daha önce yüklenmiş.");
            }
        }

        var dosyaAdi = $"{Guid.NewGuid()}{uzanti}";
        var dosyaYolu = Path.Combine(uploadsPath, dosyaAdi);

        await using var stream = new FileStream(dosyaYolu, FileMode.Create);
        await file.CopyToAsync(stream);

        return Ok(new { url = $"/uploads/{dosyaAdi}" });
    }

    [HttpGet("fotoğraf/indir/{dosyaAdi}")]
    [EnableRateLimiting("read")]
    public IActionResult FotografIndir(string dosyaAdi)
    {
        var guvenliDosyaAdi = Path.GetFileName(dosyaAdi);
        var dosyaYolu = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            guvenliDosyaAdi);

        if (!System.IO.File.Exists(dosyaYolu))
        {
            throw new NotFoundException("PROFILE_PHOTO_NOT_FOUND", "Fotoğraf bulunamadı.");
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(dosyaYolu, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(dosyaYolu, contentType, guvenliDosyaAdi);
    }

    [HttpGet]
    [EnableRateLimiting("read")]
    public IActionResult Getir(
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5)
    {
        var musteriler = _service.Getir(search, sort, page, pageSize);
        return Ok(new DataResult<object>(true, _localizer["CustomersRetrieved"], musteriler));
    }

    [HttpGet("excel")]
    [Authorize(Policy = "AdminOnly")]
    [EnableRateLimiting("read")]
    public IActionResult ExcelAktar(
        [FromQuery] string? search = null,
        [FromQuery] string? sort = null)
    {
        var musteriler = _service.Getir(search, sort, 1, 10000);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Müşteriler");

        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Ad";
        worksheet.Cell(1, 3).Value = "Soyad";
        worksheet.Cell(1, 4).Value = "Telefon";
        worksheet.Cell(1, 5).Value = "E-posta";

        for (var i = 0; i < musteriler.Count; i++)
        {
            var musteri = musteriler[i];
            var row = i + 2;

            worksheet.Cell(row, 1).Value = musteri.Id;
            worksheet.Cell(row, 2).Value = musteri.Ad;
            worksheet.Cell(row, 3).Value = musteri.Soyad;
            worksheet.Cell(row, 4).Value = musteri.Telefon;
            worksheet.Cell(row, 5).Value = musteri.Email;
        }

        worksheet.Range(1, 1, 1, 5).Style.Font.Bold = true;
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "musteriler.xlsx");
    }

    [HttpGet("cursor")]
    [EnableRateLimiting("read")]
    public IActionResult CursorIleGetir(
        [FromQuery] int? lastId = null,
        [FromQuery] int pageSize = 5)
    {
        var musteriler = _service.CursorIleGetir(lastId, pageSize);
        var nextCursor = musteriler.Count > 0 ? musteriler.Last().Id : (int?)null;

        return Ok(new DataResult<object>(
            true,
            _localizer["CustomersRetrieved"],
            new { items = musteriler, nextCursor }));
    }

    [HttpGet("{id}")]
    [EnableRateLimiting("read")]
    public IActionResult IdyeGoreGetir(int id)
    {
        var musteri = _service.IdyeGoreGetir(id);

        if (musteri is null)
        {
            throw new NotFoundException(
                "CUSTOMER_NOT_FOUND",
                _localizer["CustomerNotFound"]);
        }

        return Ok(new DataResult<Musteri>(
            true,
            _localizer["CustomerRetrieved"],
            musteri));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("write")]
    public IActionResult Ekle(Musteri musteri)
    {
        ValidateMusteri(musteri);

        if (_service.TcKimlikNoVarMi(musteri.TcKimlikNo!))
        {
            throw new BusinessRuleException(
                "DUPLICATE_NATIONAL_ID",
                "Bu T.C. Kimlik No ile kayıtlı bir müşteri zaten var.");
        }

        _service.Ekle(musteri);

        return Ok(new DataResult<Musteri>(
            true,
            _localizer["CustomerAdded"],
            musteri));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("write")]
    public IActionResult Guncelle(int id, Musteri musteri)
    {
        ValidateMusteri(musteri);

        if (_service.TcKimlikNoVarMi(musteri.TcKimlikNo!, id))
        {
            throw new BusinessRuleException(
                "DUPLICATE_NATIONAL_ID",
                "Bu T.C. Kimlik No başka bir müşteriye ait.");
        }

        var mevcutMusteri = _service.IdyeGoreGetir(id);
        if (mevcutMusteri is null)
        {
            throw new NotFoundException(
                "CUSTOMER_NOT_FOUND",
                _localizer["CustomerToUpdateNotFound"]);
        }

        mevcutMusteri.Ad = musteri.Ad;
        mevcutMusteri.Soyad = musteri.Soyad;
        mevcutMusteri.Telefon = musteri.Telefon;
        mevcutMusteri.Email = musteri.Email;
        mevcutMusteri.TcKimlikNo = musteri.TcKimlikNo;
        mevcutMusteri.DogumTarihi = musteri.DogumTarihi;
        mevcutMusteri.ProfilFotoUrl = musteri.ProfilFotoUrl;

        _service.Guncelle(mevcutMusteri);

        return Ok(new DataResult<Musteri>(
            true,
            _localizer["CustomerUpdated"],
            mevcutMusteri));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("write")]
    public IActionResult Sil(int id)
    {
        var mevcutMusteri = _service.IdyeGoreGetir(id);
        if (mevcutMusteri is null)
        {
            throw new NotFoundException(
                "CUSTOMER_NOT_FOUND",
                _localizer["CustomerToDeleteNotFound"]);
        }

        _service.Sil(id);

        return Ok(new DataResult<object>(
            true,
            _localizer["CustomerDeleted"]));
    }

    private void ValidateMusteri(Musteri musteri)
    {
        var validationResult = _validator.Validate(musteri);

        if (!validationResult.IsValid)
        {
            var validationErrors = validationResult.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());

            throw new ApiValidationException("VALIDATION_ERROR", validationErrors);
        }
    }

    private static ApiValidationException ValidationError(
        string propertyName,
        string errorMessage)
    {
        return new ApiValidationException(
            "VALIDATION_ERROR",
            new Dictionary<string, string[]>
            {
                [propertyName] = [errorMessage]
            });
    }
}
