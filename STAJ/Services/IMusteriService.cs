using STAJ.Entities;

namespace STAJ.Services
{
    /// <summary>
    /// Müşteri uygulama servisinin sözleşmesi. Controller katmanı somut implementasyona bağlı değildir.
    /// </summary>
    public interface IMusteriService
    {
        void Ekle(Musteri musteri);
        bool TcKimlikNoVarMi(string tcKimlikNo, int? haricId = null);
        List<Musteri> Getir(string? search = null, string? sort = null, int page = 1, int pageSize = 5);
        List<Musteri> CursorIleGetir(int? lastId = null, int pageSize = 5);
        Musteri? IdyeGoreGetir(int id);
        void Guncelle(Musteri musteri);
        void Sil(int id);
    }
}
