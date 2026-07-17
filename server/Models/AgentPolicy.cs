using System;

namespace OneSecurity.Server.Models
{
    public class AgentPolicy
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public int HeartbeatInterval { get; set; } = 10;
        public int MetricsInterval { get; set; } = 10;
        public required string EnabledLogs { get; set; } // Comma-separated categories
        public bool ResponseEnabled { get; set; } = true;
        public string? Description { get; set; }
        public int Version { get; set; } = 1;
    }
}
