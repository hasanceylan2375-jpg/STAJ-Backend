using Microsoft.Extensions.Caching.Memory;
using STAJ.Entities;
using STAJ.Repositories;

namespace STAJ.Services
{
    public class MusteriService : IMusteriService
    {
        private const int CacheMinutes = 5;
        private const string CacheVersionKey = "MusteriCacheVersion";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;

        public MusteriService(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        public void Ekle(Musteri musteri)
        {
            _unitOfWork.Musteriler.Ekle(musteri);
            CacheTemizle();
        }

        public bool TcKimlikNoVarMi(string tcKimlikNo, int? haricId = null)
            => _unitOfWork.Musteriler.TcKimlikNoVarMi(tcKimlikNo, haricId);

        public List<Musteri> Getir(string? search = null, string? sort = null, int page = 1, int pageSize = 5)
            => GetOrCreate($"Musteriler_{CacheVersion}_{search}_{sort}_{page}_{pageSize}", () =>
                _unitOfWork.Musteriler.Getir(search, sort, page, pageSize));

        public List<Musteri> CursorIleGetir(int? lastId = null, int pageSize = 5)
            => GetOrCreate($"MusteriCursor_{CacheVersion}_{lastId}_{pageSize}", () =>
                _unitOfWork.Musteriler.CursorIleGetir(lastId, pageSize));

        public Musteri? IdyeGoreGetir(int id)
            => _cache.GetOrCreate($"Musteri_{id}", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheMinutes);
                return _unitOfWork.Musteriler.IdyeGoreGetir(id);
            });

        public void Guncelle(Musteri musteri)
        {
            ExecuteTransaction(() => _unitOfWork.Musteriler.Guncelle(musteri));
            _cache.Remove($"Musteri_{musteri.Id}");
            CacheTemizle();
        }

        public void Sil(int id)
        {
            ExecuteTransaction(() => _unitOfWork.Musteriler.Sil(id));
            _cache.Remove($"Musteri_{id}");
            CacheTemizle();
        }

        private List<Musteri> GetOrCreate(string key, Func<List<Musteri>> factory)
            => _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheMinutes);
                return factory();
            })!;

        private int CacheVersion => _cache.GetOrCreate(CacheVersionKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return 1;
        });

        private void CacheTemizle()
            => _cache.Set(CacheVersionKey, CacheVersion + 1, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });

        private void ExecuteTransaction(Action operation)
        {
            try
            {
                _unitOfWork.BeginTransaction();
                operation();
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
