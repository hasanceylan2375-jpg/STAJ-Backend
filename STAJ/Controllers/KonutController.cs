using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using STAJ.Data;
using STAJ.Entities;

namespace STAJ.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class KonutController : ControllerBase
    {
        private readonly AppDbContext _context;
        public KonutController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Getir([FromQuery] string? search = null)
        {
            var query = _context.Konutlar.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Baslik.ToLower().Contains(search.ToLower()) || (x.Konum ?? "").ToLower().Contains(search.ToLower()));
            return Ok(await query.OrderBy(x => x.Id).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Ekle(Konut konut) { _context.Konutlar.Add(konut); await _context.SaveChangesAsync(); return Ok(konut); }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Guncelle(int id, Konut konut)
        {
            var mevcut = await _context.Konutlar.FindAsync(id); if (mevcut == null) return NotFound();
            mevcut.Baslik = konut.Baslik; mevcut.Konum = konut.Konum; mevcut.Fiyat = konut.Fiyat; mevcut.OdaSayisi = konut.OdaSayisi; mevcut.GorselUrl = konut.GorselUrl;
            await _context.SaveChangesAsync(); return Ok(mevcut);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Sil(int id) { var mevcut = await _context.Konutlar.FindAsync(id); if (mevcut == null) return NotFound(); _context.Konutlar.Remove(mevcut); await _context.SaveChangesAsync(); return Ok(); }
    }
}
