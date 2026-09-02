using STAJ.Data;

namespace STAJ.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

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
    }
}
