using System;

namespace OneSecurity.Server.DTOs
{
    public class EnrichedHeartbeatDto
    {
        public string AgentId { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string CollectorId { get; set; } = string.Empty;
        public DateTime ReceivedTime { get; set; }
    }

    public class EnrichedMetricDto
    {
        public string AgentId { get; set; } = string.Empty;
        public decimal CpuUsagePercent { get; set; }
        public decimal RamUsagePercent { get; set; }
        public decimal DiskUsagePercent { get; set; }
        public long NetworkInBytes { get; set; }
        public long NetworkOutBytes { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string CollectorId { get; set; } = string.Empty;
        public DateTime ReceivedTime { get; set; }
    }

    public class EnrichedSecurityEventDto
    {
        public string EventId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string RawData { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string CollectorId { get; set; } = string.Empty;
        public DateTime ReceivedTime { get; set; }
    }
}
