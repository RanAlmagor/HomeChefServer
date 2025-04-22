using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using HomeChefServer.SignalR; // אם NotificationHub נמצא שם

namespace HomeChefServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestNotificationController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public TestNotificationController(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> SendTestNotification(string userId)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                content = $"🔔 הודעת בדיקה ל-User {userId}"
            });

            return Ok("Notification sent");
        }
    }
}
