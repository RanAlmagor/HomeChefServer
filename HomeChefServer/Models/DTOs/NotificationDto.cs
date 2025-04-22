using Microsoft.AspNetCore.SignalR;

namespace HomeChefServer.Models.DTOs
{
    public class NotificationDto
    {
        public string FromUserId { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
    }

}
