﻿using Microsoft.AspNetCore.SignalR;

namespace HomeChefServer.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string RecipientUserId { get; set; }
        public string? FromUserId { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

