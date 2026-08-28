using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult Getir([FromQuery] string? search = null, [FromQuery] string? sort = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
        {
            var musteriler = _service.Getir(search, sort, page, pageSize);
            return Ok(new DataResult<object>(true, _localizer["CustomersRetrieved"], musteriler));
        }

        [HttpGet("cursor")]
        public IActionResult CursorIleGetir([FromQuery] int? lastId = null, [FromQuery] int pageSize = 5)
        {
            var musteriler = _service.CursorIleGetir(lastId, pageSize);
            var nextCursor = musteriler.Count > 0 ? musteriler.Last().Id : (int?)null;
            return Ok(new DataResult<object>(true, _localizer["CustomersRetrieved"], new { items = musteriler, nextCursor }));
        }

        [HttpGet("{id}")]
        public IActionResult IdyeGoreGetir(int id)
        {
            var musteri = _service.IdyeGoreGetir(id);
            if (musteri == null) return NotFound(new DataResult<object>(false, _localizer["CustomerNotFound"]));
            return Ok(new DataResult<Musteri>(true, _localizer["CustomerRetrieved"], musteri));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Ekle(Musteri musteri)
        {
            _service.Ekle(musteri);
            return Ok(new DataResult<Musteri>(true, _localizer["CustomerAdded"], musteri));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
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
        public IActionResult Sil(int id)
        {
            var mevcutMusteri = _service.IdyeGoreGetir(id);
            if (mevcutMusteri == null) return NotFound(new DataResult<object>(false, _localizer["CustomerToDeleteNotFound"]));
            _service.Sil(id);
            return Ok(new DataResult<object>(true, _localizer["CustomerDeleted"]));
        }
    }
}
