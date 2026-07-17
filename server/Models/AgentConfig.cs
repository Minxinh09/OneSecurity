using System;
using System.Collections.Generic;

namespace OneSecurity.Server.Models
{
    public class AgentConfig
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public int HeartbeatIntervalSeconds { get; set; }
        public int MetricsIntervalSeconds { get; set; }
        public required string MonitoredLogsConfig { get; set; } // JSON String
        public int Version { get; set; }
        public bool IsDefault { get; set; } // Xác định cấu hình mặc định rõ ràng
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties (Only reference the 6 allowed entities)
        public ICollection<Agent> Agents { get; set; } = new List<Agent>();
    }
}
