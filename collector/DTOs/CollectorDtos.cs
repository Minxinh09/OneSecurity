using System;
using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Collector.DTOs
{
    public class RegisterAgentRequest
    {
        [Required(ErrorMessage = "Hostname is required")]
        [StringLength(255)]
        public required string Hostname { get; set; }

        [Required(ErrorMessage = "IP Address is required")]
        [StringLength(45)]
        public required string IpAddress { get; set; }

        [Required(ErrorMessage = "OS Info is required")]
        [StringLength(255)]
        public required string OsInfo { get; set; }

        public string? SupportedActions { get; set; }
        public string? Capabilities { get; set; }
        public string? AgentVersion { get; set; }
        public string? CollectorVersion { get; set; }
        public string? EnrollmentToken { get; set; }
        public long CollectorId { get; set; }
    }

    public class RegisterAgentResponse
    {
        public required string AgentId { get; set; }
        public required string Hostname { get; set; }
        public required string Status { get; set; }
        public int HeartbeatIntervalSeconds { get; set; }
        public DateTime RegisteredAt { get; set; }
    }

    public class HeartbeatRequest
    {
        [Required(ErrorMessage = "AgentId is required")]
        [StringLength(36)]
        public required string AgentId { get; set; }
        
        public string? MessageId { get; set; }
        public string? Timestamp { get; set; }
    }

    public class HeartbeatResponse
    {
        public required string AgentId { get; set; }
        public required string Status { get; set; }
        public DateTime LastSeenAt { get; set; }
    }

    public class MetricRequest
    {
        [Required(ErrorMessage = "AgentId is required")]
        [StringLength(36)]
        public required string AgentId { get; set; }

        public decimal CpuUsagePercent { get; set; }
        public decimal RamUsagePercent { get; set; }
        public decimal DiskUsagePercent { get; set; }
        public long NetworkInBytes { get; set; }
        public long NetworkOutBytes { get; set; }
        
        public string? MessageId { get; set; }
        public string? Timestamp { get; set; }
    }

    public class MetricResponse
    {
        public long MetricRecordId { get; set; }
        public required string AgentId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SecurityEventRequest
    {
        [Required(ErrorMessage = "EventId is required")]
        [StringLength(50)]
        public required string EventId { get; set; }

        [Required(ErrorMessage = "AgentId is required")]
        [StringLength(36)]
        public required string AgentId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(100)]
        public required string Category { get; set; }

        [Required(ErrorMessage = "Severity is required")]
        [StringLength(20)]
        public required string Severity { get; set; }

        [Required(ErrorMessage = "Source is required")]
        [StringLength(100)]
        public required string Source { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255)]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Details is required")]
        public required string Details { get; set; }

        [Required(ErrorMessage = "RawData is required")]
        public required string RawData { get; set; } // JSON
        
        public string? MessageId { get; set; }
        public string? Timestamp { get; set; }
    }

    public class SecurityEventResponse
    {
        public long Id { get; set; }
        public required string EventId { get; set; }
        public required string AgentId { get; set; }
        public DateTime ReceivedAt { get; set; }
    }

    // Enriched DTOs sent from Collector to Server in Batches
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
