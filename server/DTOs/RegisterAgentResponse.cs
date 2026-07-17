using System;

namespace OneSecurity.Server.DTOs
{
    public class RegisterAgentResponse
    {
        public required string AgentId { get; set; }
        public required string Hostname { get; set; }
        public required string Status { get; set; }
        public int HeartbeatIntervalSeconds { get; set; }
        public DateTime RegisteredAt { get; set; }
    }
}
