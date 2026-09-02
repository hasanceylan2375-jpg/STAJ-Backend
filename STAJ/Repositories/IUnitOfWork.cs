namespace STAJ.Repositories
{
    public interface IUnitOfWork
    {
        IMusteriRepository Musteriler { get; }
        int SaveChanges();
        void BeginTransaction();
        void Commit();
        void Rollback();
    }
}
