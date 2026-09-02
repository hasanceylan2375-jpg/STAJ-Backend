using Microsoft.EntityFrameworkCore.Storage;
using STAJ.Data;

namespace STAJ.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        public IMusteriRepository Musteriler { get; }

        public UnitOfWork(AppDbContext context, IMusteriRepository musteriRepository)
        {
            _context = context;
            Musteriler = musteriRepository;
        }

        public int SaveChanges()
        {
            return _context.SaveChanges();
        }

        public void BeginTransaction()
        {
            _transaction = _context.Database.BeginTransaction();
        }

        public void Commit()
        {
            SaveChanges();
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
        }
    }
}
