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
    public class SirketController : ControllerBase
    {
        private readonly AppDbContext _context;
        public SirketController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Getir([FromQuery] string? search = null)
        {
            var query = _context.Sirketler.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(x => x.Ad.ToLower().Contains(search.ToLower()));
            return Ok(await query.OrderBy(x => x.Id).ToListAsync());
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Ekle(Sirket sirket)
        {
            _context.Sirketler.Add(sirket);
            await _context.SaveChangesAsync();
            return Ok(sirket);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Guncelle(int id, Sirket sirket)
        {
            var mevcut = await _context.Sirketler.FindAsync(id);
            if (mevcut == null) return NotFound();
            mevcut.Ad = sirket.Ad; mevcut.LogoUrl = sirket.LogoUrl; mevcut.Sektor = sirket.Sektor; mevcut.Email = sirket.Email; mevcut.Telefon = sirket.Telefon;
            await _context.SaveChangesAsync();
            return Ok(mevcut);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Sil(int id)
        {
            var mevcut = await _context.Sirketler.FindAsync(id);
            if (mevcut == null) return NotFound();
            _context.Sirketler.Remove(mevcut);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
