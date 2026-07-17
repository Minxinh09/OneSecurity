using System;
using System.Collections.Generic;

namespace OneSecurity.Server.DTOs
{
    public class DashboardOverviewDto
    {
        public int OpenIncidents { get; set; }
        public int CriticalIncidents { get; set; }
        public int OnlineAgents { get; set; }
        public int OfflineAgents { get; set; }
        public int AlertsToday { get; set; }
        public int ResolvedToday { get; set; }
        public int AssignedToMe { get; set; }

        public List<DashboardTrendItem> AlertTrend { get; set; } = new();
        public List<DashboardTrendItem> IncidentTrend { get; set; } = new();
        public List<DashboardKeyValueItem> AlertSeverityDistribution { get; set; } = new();
        public List<DashboardKeyValueItem> TopAlertRules { get; set; } = new();
        public List<DashboardKeyValueItem> TopAffectedHosts { get; set; } = new();
        public List<RecentActivityItemDto> RecentActivities { get; set; } = new();
    }

    public class DashboardTrendItem
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DashboardKeyValueItem
    {
        public string Key { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public class RecentActivityItemDto
    {
        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    public class ThreatSearchResultItemDto
    {
        public string Type { get; set; } = string.Empty; // "SecurityEvent", "Alert", "Incident", "AuditLog", "Agent"
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ThreatSearchResultDto
    {
        public List<ThreatSearchResultItemDto> Items { get; set; } = new();
    }

    public class ThreatSearchRequest
    {
        public string? Keyword { get; set; }
        public string? Hostname { get; set; }
        
        [Microsoft.AspNetCore.Mvc.FromQuery(Name = "ip")]
        public string? IPAddress { get; set; }
        
        public string? Username { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class DashboardOverviewUpdatedDto
    {
        public int OnlineAgents { get; set; }
        public int OfflineAgents { get; set; }
        public int TotalAgents { get; set; }
        public int OpenIncidents { get; set; }
        public int CriticalIncidents { get; set; }
        public int AlertsToday { get; set; }
        public int ResolvedToday { get; set; }
        public int AssignedToMe { get; set; }
    }

    public class TimelineItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "SecurityEvent", "Alert", "Incident", "Audit"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? UserName { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
    }
}
