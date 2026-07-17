using System;

namespace OneSecurity.Server.DTOs
{
    public class AlertNotificationDto
    {
        public long Id { get; set; }
        public required string AgentId { get; set; }
        public required string AgentHostname { get; set; }
        public required string RuleName { get; set; }
        public required string Severity { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public required string Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public int HospitalId { get; set; }
    }

    public class HeartbeatNotificationDto
    {
        public required string AgentId { get; set; }
        public required string AgentHostname { get; set; }
        public string? IpAddress { get; set; }
        public required string Status { get; set; }
        public DateTime LastSeenAt { get; set; }
        public int HospitalId { get; set; }
    }

    public class MetricNotificationDto
    {
        public long Id { get; set; }
        public required string AgentId { get; set; }
        public required string AgentHostname { get; set; }
        public decimal CpuUsagePercent { get; set; }
        public decimal RamUsagePercent { get; set; }
        public decimal DiskUsagePercent { get; set; }
        public long NetworkInBytes { get; set; }
        public long NetworkOutBytes { get; set; }
        public DateTime Timestamp { get; set; }
        public int HospitalId { get; set; }
    }

    public class SecurityEventNotificationDto
    {
        public long Id { get; set; }
        public required string EventId { get; set; }
        public required string AgentId { get; set; }
        public required string AgentHostname { get; set; }
        public required string Category { get; set; }
        public required string Severity { get; set; }
        public required string Source { get; set; }
        public required string Title { get; set; }
        public required string Details { get; set; }
        public DateTime Timestamp { get; set; }
        public int HospitalId { get; set; }
    }

    public class AgentStatusNotificationDto
    {
        public required string AgentId { get; set; }
        public required string AgentHostname { get; set; }
        public string? IpAddress { get; set; }
        public required string OldStatus { get; set; }
        public required string NewStatus { get; set; }
        public DateTime Timestamp { get; set; }
        public int HospitalId { get; set; }
    }

    public class IncidentNotificationDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public int AlertCount { get; set; }
        public DateTime Timestamp { get; set; }
        public int HospitalId { get; set; }
    }

    public class ResponseActionNotificationDto
    {
        public long Id { get; set; }
        public long IncidentId { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;
        public string? ResultMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public int HospitalId { get; set; }
    }
}