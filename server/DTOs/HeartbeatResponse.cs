using System;

namespace OneSecurity.Server.DTOs
{
    public class HeartbeatResponse
    {
        public required string AgentId { get; set; }
        public required string Status { get; set; }
        public DateTime LastSeenAt { get; set; }
    }
}
