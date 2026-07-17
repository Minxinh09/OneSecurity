using System;

namespace OneSecurity.Server.DTOs
{
    public class AgentStatusDto
    {
        public required string AgentId { get; set; }
        public required string Hostname { get; set; }
        public required string IpAddress { get; set; }
        public required string Status { get; set; }
        public DateTime LastSeenAt { get; set; }
    }
}
