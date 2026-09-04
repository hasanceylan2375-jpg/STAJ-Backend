using Microsoft.Extensions.Caching.Memory;
using STAJ.Entities;
using STAJ.Repositories;

namespace STAJ.Services
{
    public class MusteriService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private const string CacheVersionKey = "MusteriCacheVersion";

        public MusteriService(IUnitOfWork unitOfWork, IMemoryCache cache)
        {
            _unitOfWork = unitOfWork;
            _cache = cache;
        }

        private int CacheVersion => _cache.GetOrCreate(CacheVersionKey, entry =>
        {
            entry.Priority = CacheItemPriority.NeverRemove;
            return 1;
        });

        private void CacheTemizle()
        {
            _cache.Set(CacheVersionKey, CacheVersion + 1, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.NeverRemove
            });
        }

        public void Ekle(Musteri musteri)
        {
            _unitOfWork.Musteriler.Ekle(musteri);
            CacheTemizle();
        }

        public bool TcKimlikNoVarMi(string tcKimlikNo, int? haricId = null)
            => _unitOfWork.Musteriler.TcKimlikNoVarMi(tcKimlikNo, haricId);

        public List<Musteri> Getir(string? search = null, string? sort = null, int page = 1, int pageSize = 5)
        {
            var cacheKey = $"Musteriler_{CacheVersion}_{search}_{sort}_{page}_{pageSize}";
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return _unitOfWork.Musteriler.Getir(search, sort, page, pageSize);
            })!;
        }

        public List<Musteri> CursorIleGetir(int? lastId = null, int pageSize = 5)
        {
            var cacheKey = $"MusteriCursor_{CacheVersion}_{lastId}_{pageSize}";
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return _unitOfWork.Musteriler.CursorIleGetir(lastId, pageSize);
            })!;
        }

        public Musteri? IdyeGoreGetir(int id)
        {
            var cacheKey = $"Musteri_{id}";
            return _cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return _unitOfWork.Musteriler.IdyeGoreGetir(id);
            });
        }

        public void Guncelle(Musteri musteri)
        {
            try
            {
                _unitOfWork.BeginTransaction();
                _unitOfWork.Musteriler.Guncelle(musteri);
                _unitOfWork.Commit();
                _cache.Remove($"Musteri_{musteri.Id}");
                CacheTemizle();
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
                _cache.Remove($"Musteri_{id}");
                CacheTemizle();
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        }
    }
}
