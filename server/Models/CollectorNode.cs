using System;

namespace OneSecurity.Server.Models
{
    public class CollectorNode
    {
        // Primary Key
        public long Id { get; set; }

        // Core Identity
        public required string Name { get; set; }
        public required string IPAddress { get; set; }
        public required string CollectorKey { get; set; }
        public required string SharedSecret { get; set; }

        // Multi-Collector Architecture (New)
        public int HospitalId { get; set; }
        public int ConnectedAgents { get; set; } = 0;
        public int QueueSize { get; set; } = 0;

        // Monitoring & Status
        public required string Version { get; set; } = "1.0.0";
        public required string Status { get; set; } = "Offline"; // Online, Offline
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
        public DateTime LastSync { get; set; } = DateTime.UtcNow;

        // Metadata
        public string? Location { get; set; }
        public string? Description { get; set; }
        public int ConfigurationVersion { get; set; } = 1;
        public int RulesVersion { get; set; } = 1;
    }
}