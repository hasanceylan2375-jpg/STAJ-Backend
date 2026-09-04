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
    public class AracController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AracController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Getir([FromQuery] string? search = null)
        {
            var query = _context.Araclar.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Marka.ToLower().Contains(search.ToLower()) || x.Model.ToLower().Contains(search.ToLower()));
            return Ok(await query.OrderBy(x => x.Id).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Ekle(Arac arac) { _context.Araclar.Add(arac); await _context.SaveChangesAsync(); return Ok(arac); }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Guncelle(int id, Arac arac)
        {
            var mevcut = await _context.Araclar.FindAsync(id); if (mevcut == null) return NotFound();
            mevcut.Marka = arac.Marka; mevcut.Model = arac.Model; mevcut.Yil = arac.Yil; mevcut.Fiyat = arac.Fiyat; mevcut.GorselUrl = arac.GorselUrl;
            await _context.SaveChangesAsync(); return Ok(mevcut);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Sil(int id) { var mevcut = await _context.Araclar.FindAsync(id); if (mevcut == null) return NotFound(); _context.Araclar.Remove(mevcut); await _context.SaveChangesAsync(); return Ok(); }
    }
}
