using STAJ.Entities;
using STAJ.Repositories;

namespace STAJ.Services
{
    public class MusteriService
    {
        private readonly IUnitOfWork _unitOfWork;

        public MusteriService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void Ekle(Musteri musteri)
        {
            try
            {
                _unitOfWork.BeginTransaction();
                _unitOfWork.Musteriler.Ekle(musteri);
                _unitOfWork.Commit();
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        }

        public bool TcKimlikNoVarMi(string tcKimlikNo, int? haricId = null)
            => _unitOfWork.Musteriler.TcKimlikNoVarMi(tcKimlikNo, haricId);

        public List<Musteri> Getir(string? search = null, string? sort = null, int page = 1, int pageSize = 5)
        {
            return _unitOfWork.Musteriler.Getir(search, sort, page, pageSize);
        }

        public List<Musteri> CursorIleGetir(int? lastId = null, int pageSize = 5)
        {
            return _unitOfWork.Musteriler.CursorIleGetir(lastId, pageSize);
        }

        public Musteri? IdyeGoreGetir(int id) => _unitOfWork.Musteriler.IdyeGoreGetir(id);

        public void Guncelle(Musteri musteri)
        {
            try
            {
                _unitOfWork.BeginTransaction();
                _unitOfWork.Musteriler.Guncelle(musteri);
                _unitOfWork.Commit();
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        }

        public void Sil(int id)
        {
            try
            {
                _unitOfWork.BeginTransaction();
                _unitOfWork.Musteriler.Sil(id);
                _unitOfWork.Commit();
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        }
    }
}
