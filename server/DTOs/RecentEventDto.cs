using System;

namespace OneSecurity.Server.DTOs
{
    public class RecentEventDto
    {
        public long Id { get; set; }
        public required string EventId { get; set; }
        public required string AgentId { get; set; }
        public string? AgentHostname { get; set; }
        public DateTime Timestamp { get; set; }
        public required string Category { get; set; }
        public required string Severity { get; set; }
        public required string Source { get; set; }
        public required string Title { get; set; }
        public required string Details { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
