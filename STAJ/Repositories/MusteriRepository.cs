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
            _context.Musteriler.Update(musteri);
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
        public List<Musteri> Getir()
        {
            return _context.Musteriler.ToList();
        }
    }
}
