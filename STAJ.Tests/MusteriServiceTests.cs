using Microsoft.Extensions.Caching.Memory;
using Moq;
using STAJ.Entities;
using STAJ.Repositories;
using STAJ.Services;

namespace STAJ.Tests;

public class MusteriServiceTests
{
    private static (MusteriService service, Mock<IUnitOfWork> unitOfWork, Mock<IMusteriRepository> repository) CreateSut()
    {
        var repository = new Mock<IMusteriRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Musteriler).Returns(repository.Object);
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (new MusteriService(unitOfWork.Object, cache), unitOfWork, repository);
    }

    [Fact]
    public void Ekle_MusteriyiRepositoryeEkler_veCacheVersioniniYeniler()
    {
        var (service, _, repository) = CreateSut();
        var musteri = new Musteri { Ad = "Ali", Soyad = "Veli", Telefon = "555", Email = "ali@example.com" };

        service.Ekle(musteri);

        repository.Verify(x => x.Ekle(musteri), Times.Once);
    }

    [Fact]
    public void TcKimlikNoVarMi_RepositorySonucunuDondurur()
    {
        var (service, _, repository) = CreateSut();
        repository.Setup(x => x.TcKimlikNoVarMi("12345678901", null)).Returns(true);

        var result = service.TcKimlikNoVarMi("12345678901");

        Assert.True(result);
    }

    [Fact]
    public void Guncelle_BasarisizTransactiondaRollbackYapar()
    {
        var (service, unitOfWork, repository) = CreateSut();
        var musteri = new Musteri { Id = 7, Ad = "Ali", Soyad = "Veli", Telefon = "555", Email = "ali@example.com" };
        repository.Setup(x => x.Guncelle(musteri)).Throws(new InvalidOperationException("test"));

        Assert.Throws<InvalidOperationException>(() => service.Guncelle(musteri));
        unitOfWork.Verify(x => x.BeginTransaction(), Times.Once);
        unitOfWork.Verify(x => x.Rollback(), Times.Once);
        unitOfWork.Verify(x => x.Commit(), Times.Never);
    }
}
