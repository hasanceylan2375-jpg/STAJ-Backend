using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace STAJ.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.Caller.SendAsync("baglantiKuruldu", new
            {
                connectionId = Context.ConnectionId,
                message = "Gerçek zamanlı bağlantı kuruldu."
            });

            await base.OnConnectedAsync();
        }
    }
}
