using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace OneSecurity.Server.Models
{
    public class Agent
    {
        public required string Id { get; set; } // UUID v4
        public required string Hostname { get; set; }
        public required string IpAddress { get; set; }
        public required string OsInfo { get; set; }
        public string? HardwareSpecs { get; set; } // JSON String
        public required string Status { get; set; } = "inactive";
        
        public long ConfigId { get; set; }
        public AgentConfig? Config { get; set; } // Navigation Property

        public long? AssetId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public InfrastructureAsset? Asset { get; set; } // Link to Asset (1-to-N)

        public int? HospitalId { get; set; }
        [ForeignKey(nameof(HospitalId))]
        public Hospital? Hospital { get; set; }
        public string? SupportedActions { get; set; }
        public string? Capabilities { get; set; }
        public string? AgentVersion { get; set; }
        public string? CollectorVersion { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties (Only reference the 6 allowed entities)
        public ICollection<MetricRecord> MetricRecords { get; set; } = new List<MetricRecord>();
        public ICollection<SecurityEvent> SecurityEvents { get; set; } = new List<SecurityEvent>();
        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}
