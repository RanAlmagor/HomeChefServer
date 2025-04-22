using Microsoft.AspNetCore.SignalR;
using HomeChefServer.Models.DTOs;

namespace HomeChefServer.Services
{
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(IHubContext<NotificationHub> hub)
        {
            _hub = hub;
        }

        public async Task SendNotificationAsync(string recipientUserId, NotificationDto dto)
        {
            await _hub.Clients.User(recipientUserId).SendAsync("ReceiveNotification", dto);
        }
    }
}
