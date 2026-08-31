using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using STAJ.Entities;
using STAJ.Results;
using STAJ.Resources;
using STAJ.Services;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MusteriController : ControllerBase
    {
        private readonly MusteriService _service;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public MusteriController(MusteriService service, IStringLocalizer<SharedResource> localizer)
        {
            _service = service;
            _localizer = localizer;
        }

        [HttpGet]
        [EnableRateLimiting("read")]
        public IActionResult Getir([FromQuery] string? search = null, [FromQuery] string? sort = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            var musteriler = _service.Getir(search, sort, page, pageSize);
            return Ok(new DataResult<object>(true, _localizer["CustomersRetrieved"], musteriler));
        }

        [HttpGet("excel")]
        [EnableRateLimiting("read")]
        public IActionResult ExcelAktar([FromQuery] string? search = null, [FromQuery] string? sort = null)
        {
            var musteriler = _service.Getir(search, sort, 1, 10000);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Müşteriler");

            worksheet.Cell(1, 1).Value = "ID";
            worksheet.Cell(1, 2).Value = "Ad";
            worksheet.Cell(1, 3).Value = "Soyad";
            worksheet.Cell(1, 4).Value = "Telefon";
            worksheet.Cell(1, 5).Value = "E-posta";

            for (int i = 0; i < musteriler.Count; i++)
            {
                var musteri = musteriler[i];
                var row = i + 2;
                worksheet.Cell(row, 1).Value = musteri.Id;
                worksheet.Cell(row, 2).Value = musteri.Ad;
                worksheet.Cell(row, 3).Value = musteri.Soyad;
                worksheet.Cell(row, 4).Value = musteri.Telefon;
                worksheet.Cell(row, 5).Value = musteri.Email;
            }

            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileBytes = stream.ToArray();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "musteriler.xlsx");
        }

        [HttpGet("cursor")]
        [EnableRateLimiting("read")]
        public IActionResult CursorIleGetir([FromQuery] int? lastId = null, [FromQuery] int pageSize = 5)
        {
            var musteriler = _service.CursorIleGetir(lastId, pageSize);
            var nextCursor = musteriler.Count > 0 ? musteriler.Last().Id : (int?)null;
            return Ok(new DataResult<object>(true, _localizer["CustomersRetrieved"], new { items = musteriler, nextCursor }));
        }

        [HttpGet("{id}")]
        [EnableRateLimiting("read")]
        public IActionResult IdyeGoreGetir(int id)
        {
            var musteri = _service.IdyeGoreGetir(id);
            if (musteri == null) return NotFound(new DataResult<object>(false, _localizer["CustomerNotFound"]));
            return Ok(new DataResult<Musteri>(true, _localizer["CustomerRetrieved"], musteri));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("write")]
        public IActionResult Ekle(Musteri musteri)
        {
            _service.Ekle(musteri);
            return Ok(new DataResult<Musteri>(true, _localizer["CustomerAdded"], musteri));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("write")]
        public IActionResult Guncelle(int id, Musteri musteri)
        {
            var mevcutMusteri = _service.IdyeGoreGetir(id);
            if (mevcutMusteri == null) return NotFound(new DataResult<object>(false, _localizer["CustomerToUpdateNotFound"]));
            mevcutMusteri.Ad = musteri.Ad;
            mevcutMusteri.Soyad = musteri.Soyad;
            mevcutMusteri.Telefon = musteri.Telefon;
            mevcutMusteri.Email = musteri.Email;
            _service.Guncelle(mevcutMusteri);
            return Ok(new DataResult<Musteri>(true, _localizer["CustomerUpdated"], mevcutMusteri));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [EnableRateLimiting("write")]
        public IActionResult Sil(int id)
        {
            var mevcutMusteri = _service.IdyeGoreGetir(id);
            if (mevcutMusteri == null) return NotFound(new DataResult<object>(false, _localizer["CustomerToDeleteNotFound"]));
            _service.Sil(id);
            return Ok(new DataResult<object>(true, _localizer["CustomerDeleted"]));
        }
    }
}
