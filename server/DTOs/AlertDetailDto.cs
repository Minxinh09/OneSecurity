using System;

namespace OneSecurity.Server.DTOs
{
    public class AlertDetailDto
    {
        public long Id { get; set; }
        public required string RuleName { get; set; }
        public required string Severity { get; set; }
        public required string Category { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public string? AgentHostname { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAcknowledged { get; set; }
        public bool TelegramSent { get; set; }
    }
}
