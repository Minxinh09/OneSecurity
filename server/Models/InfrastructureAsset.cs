using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
namespace OneSecurity.Server.Models
{
    public class InfrastructureAsset
    {
        public long Id { get; set; }
        public required string Hostname { get; set; }
        public required string IPAddress { get; set; }
        public required string OperatingSystem { get; set; }
        public required string OperatingSystemVersion { get; set; }
        public string? Domain { get; set; }
        public string? Department { get; set; }
        public string? Building { get; set; }
        public string? Owner { get; set; }
        public string? Description { get; set; }
        public string Criticality { get; set; } = "Medium"; // Critical, High, Medium, Low
        public string AssetType { get; set; } = "Workstation"; // Server, Workstation, Laptop, DomainController
        public string Status { get; set; } = "Discovered"; // Discovered, PendingEnrollment, Managed, Maintenance, Retired
        
        public int? HospitalId { get; set; }
        [ForeignKey(nameof(HospitalId))]
        public Hospital? Hospital { get; set; }
        public long CollectorId { get; set; }
        public CollectorNode? Collector { get; set; }
        
        public long PolicyId { get; set; }
        public AgentPolicy? Policy { get; set; }

       
        
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<Agent> Agents { get; set; } = new List<Agent>(); // 1-to-N relationship
        
        public DateTime? LastSeen { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
