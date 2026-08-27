using STAJ.Entities;
using STAJ.Repositories;

namespace STAJ.Services
{
    public class MusteriService
    {
        private readonly MusteriRepository _repository;

        public MusteriService(MusteriRepository repository)
        {
            _repository = repository;
        }

        public void Ekle(Musteri musteri)
        {
            _repository.Ekle(musteri);
        }

        public List<Musteri> Getir(string? search = null, string? sort = null, int page = 1, int pageSize = 5)
        {
            return _repository.Getir(search, sort, page, pageSize);
        }

        public Musteri? IdyeGoreGetir(int id)
        {
            return _repository.IdyeGoreGetir(id);
        }

        public void Guncelle(Musteri musteri)
        {
            _repository.Guncelle(musteri);
        }

        public void Sil(int id)
        {
            _repository.Sil(id);
        }
    }
}
