using System;
using System.Collections.Generic;

namespace OneSecurity.Server.Models
{
    public class AlertRule
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? EventType { get; set; }
        public required string ConditionExpression { get; set; } // JSON String
        public required string AlertSeverity { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? TelegramChatId { get; set; }
        public int Priority { get; set; } = 3;
        public string Category { get; set; } = "General";
        public int Version { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}
