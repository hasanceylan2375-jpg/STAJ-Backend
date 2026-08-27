using STAJ.Data;
using STAJ.Entities;

namespace STAJ.Repositories
{
    public class MusteriRepository
    {
        private readonly AppDbContext _context;

        public MusteriRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Ekle(Musteri musteri)
        {
            _context.Musteriler.Add(musteri);
            _context.SaveChanges();
        }

        public void Guncelle(Musteri musteri)
        {
            var mevcutMusteri = _context.Musteriler.Find(musteri.Id);

            if (mevcutMusteri == null)
                return;

            mevcutMusteri.Ad = musteri.Ad;
            mevcutMusteri.Soyad = musteri.Soyad;
            mevcutMusteri.Telefon = musteri.Telefon;
            mevcutMusteri.Email = musteri.Email;

            _context.SaveChanges();
        }

        public void Sil(int id)
        {
            var musteri = _context.Musteriler.Find(id);

            if (musteri != null)
            {
                _context.Musteriler.Remove(musteri);
                _context.SaveChanges();
            }
        }

        public Musteri? IdyeGoreGetir(int id)
        {
            return _context.Musteriler.Find(id);
        }

        public List<Musteri> Getir(string? search = null, string? sort = null, int page = 1, int pageSize = 5)
        {
            var sorgu = _context.Musteriler.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                sorgu = sorgu.Where(x =>
                    x.Ad.ToLower().Contains(search) ||
                    x.Soyad.ToLower().Contains(search));
            }

            sorgu = sort?.ToLower() switch
            {
                "ad" => sorgu.OrderBy(x => x.Ad),
                "ad_desc" => sorgu.OrderByDescending(x => x.Ad),
                "soyad" => sorgu.OrderBy(x => x.Soyad),
                "soyad_desc" => sorgu.OrderByDescending(x => x.Soyad),
                "id_desc" => sorgu.OrderByDescending(x => x.Id),
                _ => sorgu.OrderBy(x => x.Id)
            };

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 5;

            return sorgu
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
    }
}
