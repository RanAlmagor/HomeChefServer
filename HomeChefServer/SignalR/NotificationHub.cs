using Microsoft.AspNetCore.SignalR;

public class NotificationHub : Hub
{
    public async Task SendNotification(string userId, object notification)
    {
        
        await Clients.User(userId).SendAsync("ReceiveNotification", notification);
    }

    
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("UserId")?.Value;
            Console.WriteLine($"🔌 משתמש התחבר עם UserId: {userId}");

            await base.OnConnectedAsync();
        }

}
