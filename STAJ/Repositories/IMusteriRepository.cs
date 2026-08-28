using STAJ.Entities;

namespace STAJ.Repositories
{
    public interface IMusteriRepository
    {
        void Ekle(Musteri musteri);
        void Guncelle(Musteri musteri);
        void Sil(int id);
        Musteri? IdyeGoreGetir(int id);
        List<Musteri> Getir(string? search = null, string? sort = null, int page = 1, int pageSize = 5);
        List<Musteri> CursorIleGetir(int? lastId = null, int pageSize = 5);
    }
}
