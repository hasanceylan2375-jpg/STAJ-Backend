using STAJ.Entities;

namespace STAJ.Events
{
    public sealed record MusteriEklendiEvent(Musteri Musteri) : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }

    public sealed record MusteriGuncellendiEvent(Musteri Musteri) : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }

    public sealed record MusteriSilindiEvent(int Id, string Ad, string Soyad) : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }
}
