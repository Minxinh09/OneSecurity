using System;

namespace OneSecurity.Server.Models
{
    public class MetricRecord
    {
        public long Id { get; set; }
        public required string AgentId { get; set; }
        public Agent? Agent { get; set; } // Navigation Property
        public DateTime Timestamp { get; set; }
        public decimal CpuUsagePercent { get; set; }
        public decimal RamUsagePercent { get; set; }
        public decimal DiskUsagePercent { get; set; }
        public long NetworkInBytes { get; set; }
        public long NetworkOutBytes { get; set; }
    }
}
