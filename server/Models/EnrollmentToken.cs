using System;

namespace OneSecurity.Server.Models
{
    public class EnrollmentToken
    {
        public long Id { get; set; }
        public required string Token { get; set; }
        
        public long AssetId { get; set; }
        public InfrastructureAsset? Asset { get; set; }
        
        public long PolicyId { get; set; }
        public AgentPolicy? Policy { get; set; }
        
        public long CollectorId { get; set; }
        public CollectorNode? Collector { get; set; }
        
        public DateTime ExpireAt { get; set; }
        public bool Used { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Reason { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UsedAt { get; set; }
        public int MaxUses { get; set; } = 1;
        public int UsedCount { get; set; } = 0;
    }
}
