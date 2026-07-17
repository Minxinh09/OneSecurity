using System;
using System.Collections.Generic;

namespace OneSecurity.Server.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string? UserName { get; set; }
        public string? Roles { get; set; } // Matches the Roles property in the model
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Severity { get; set; } = string.Empty;
    }

    public class AuditLogDetailDto : AuditLogDto
    {
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? EntityId { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class AuditLogFilterRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? User { get; set; } // UserName
        public string? Severity { get; set; }
        public string? Action { get; set; }
        public string? ResourceType { get; set; }
        public bool? Success { get; set; }
        public int? StatusCode { get; set; }
        public string? EntityId { get; set; }

        public string SortBy { get; set; } = "TimestampUtc";
        public string SortDirection { get; set; } = "desc";
    }

    public class AuditLogListResponse
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public List<AuditLogDto> Items { get; set; } = new();
    }
}
