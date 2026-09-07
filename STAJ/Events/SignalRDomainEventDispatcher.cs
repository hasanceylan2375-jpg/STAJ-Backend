using Microsoft.AspNetCore.SignalR;
using STAJ.Hubs;

namespace STAJ.Events
{
    public sealed class SignalRDomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRDomainEventDispatcher(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
        {
            return domainEvent switch
            {
                MusteriEklendiEvent e => _hubContext.Clients.All.SendAsync("musteriEklendi", e.Musteri, cancellationToken),
                MusteriGuncellendiEvent e => _hubContext.Clients.All.SendAsync("musteriGuncellendi", e.Musteri, cancellationToken),
                MusteriSilindiEvent e => _hubContext.Clients.All.SendAsync("musteriSilindi", new { e.Id, e.Ad, e.Soyad }, cancellationToken),
                _ => Task.CompletedTask
            };
        }
    }
}
