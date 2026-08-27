using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STAJ.Entities;
using STAJ.Results;
using STAJ.Services;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MusteriController : ControllerBase
    {
        private readonly MusteriService _service;

        public MusteriController(MusteriService service)
        {
            _service = service;
        }

        [HttpGet]
        public IActionResult Getir()
        {
            var musteriler = _service.Getir();
            return Ok(new DataResult<object>(true, "Müşteriler başarıyla getirildi.", musteriler));
        }

        [HttpGet("{id}")]
        public IActionResult IdyeGoreGetir(int id)
        {
            var musteri = _service.IdyeGoreGetir(id);

            if (musteri == null)
                return NotFound(new DataResult<object>(false, "Müşteri bulunamadı."));

            return Ok(new DataResult<Musteri>(true, "Müşteri başarıyla getirildi.", musteri));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Ekle(Musteri musteri)
        {
            _service.Ekle(musteri);
            return Ok(new DataResult<Musteri>(true, "Müşteri başarıyla eklendi.", musteri));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Guncelle(int id, Musteri musteri)
        {
            var mevcutMusteri = _service.IdyeGoreGetir(id);

            if (mevcutMusteri == null)
                return NotFound(new DataResult<object>(false, "Güncellenecek müşteri bulunamadı."));

            musteri.Id = id;
            _service.Guncelle(musteri);

            return Ok(new DataResult<Musteri>(true, "Müşteri başarıyla güncellendi.", musteri));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult Sil(int id)
        {
            var mevcutMusteri = _service.IdyeGoreGetir(id);

            if (mevcutMusteri == null)
                return NotFound(new DataResult<object>(false, "Silinecek müşteri bulunamadı."));

            _service.Sil(id);
            return Ok(new DataResult<object>(true, "Müşteri başarıyla silindi."));
        }
    }
}
