using Microsoft.AspNetCore.Mvc;
using STAJ.Entities;
using STAJ.Services;
namespace STAJ.Controllers

{
    [ApiController]
    [Route("api/[controller]")]
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
            return Ok(_service.Getir());
        }

        [HttpGet("{id}")]
        public IActionResult IdyeGoreGetir(int id)
        {
            var musteri = _service.IdyeGoreGetir(id);

            if (musteri == null)
                return NotFound();

            return Ok(musteri);
        }

        [HttpPost]
        public IActionResult Ekle(Musteri musteri)
        {
            _service.Ekle(musteri);
            return Ok(musteri);
        }

        [HttpPut("{id}")]
        public IActionResult Guncelle(int id, Musteri musteri)
        {
            musteri.Id = id;
            _service.Guncelle(musteri);
            return Ok(musteri);
        }

        [HttpDelete("{id}")]
        public IActionResult Sil(int id)
        {
            _service.Sil(id);
            return NoContent();
        }

    }
}
