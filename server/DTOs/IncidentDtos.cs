using System;
using System.Collections.Generic;

namespace OneSecurity.Server.DTOs
{
    public class IncidentDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
        public string Status { get; set; } = string.Empty; // New, Assigned, etc.
        public string? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int AlertCount { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class IncidentDetailDto : IncidentDto
    {
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedBy { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? ClosedBy { get; set; }
        public List<RecentAlertDto> Alerts { get; set; } = new();
    }

    public class IncidentListResponse
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public List<IncidentDto> Items { get; set; } = new();
    }

    public class CreateIncidentRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<long> AlertIds { get; set; } = new();
    }

    public class AssignIncidentRequest
    {
        public string? AssignedUserId { get; set; }
    }

    public class UpdateIncidentStatusRequest
    {
        public string Status { get; set; } = string.Empty; // New, Assigned, Investigating, Resolved, FalsePositive, Closed
    }

    public class LinkAlertsRequest
    {
        public List<long> AlertIds { get; set; } = new();
    }
}
