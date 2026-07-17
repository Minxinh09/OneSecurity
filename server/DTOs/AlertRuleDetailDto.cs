using System;

namespace OneSecurity.Server.DTOs
{
    public class AlertRuleDetailDto
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? EventType { get; set; }
        public required string ConditionExpression { get; set; }
        public required string AlertSeverity { get; set; }
        public bool IsEnabled { get; set; }
        public string? TelegramChatId { get; set; }
        public int Priority { get; set; }
        public string Category { get; set; } = "General";
        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
