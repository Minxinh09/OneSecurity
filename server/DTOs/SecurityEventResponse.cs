using System;

namespace OneSecurity.Server.DTOs
{
    public class SecurityEventResponse
    {
        public long Id { get; set; }
        public required string EventId { get; set; }
        public required string AgentId { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
