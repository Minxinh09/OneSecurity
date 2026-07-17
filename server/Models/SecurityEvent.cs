using System;

namespace OneSecurity.Server.Models
{
    public class SecurityEvent
    {
        public long Id { get; set; }
        public required string EventId { get; set; } // UUID from agent (dedup)
        public required string AgentId { get; set; }
        public Agent? Agent { get; set; } // Navigation Property
        public DateTime Timestamp { get; set; }
        public required string Category { get; set; } // login, service, firewall, etc.
        public required string Severity { get; set; } // critical, warning, info
        public required string Source { get; set; } // eventlog, sshlog, sqlserver, etc.
        public required string Title { get; set; }
        public required string Details { get; set; }
        public required string RawData { get; set; } // JSON
        public DateTime ReceivedAt { get; set; }
    }
}
