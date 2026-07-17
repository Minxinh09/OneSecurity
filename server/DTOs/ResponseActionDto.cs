using System;

namespace OneSecurity.Server.DTOs
{
    public class ResponseActionDto
    {
        public long Id { get; set; }
        public long IncidentId { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public string AgentHostname { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string RequestedByUserId { get; set; } = string.Empty;
        public string RequestedByUserName { get; set; } = string.Empty;
        public string? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ResultMessage { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string? Metadata { get; set; }
        
        public string? Parameters { get; set; }
        public string? ErrorMessage { get; set; }
        public int? HospitalId { get; set; }
        public string? CreatedBy { get; set; }
        public string? Output { get; set; }
    }

    public class ResponseActionListResponse
    {
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public System.Collections.Generic.List<ResponseActionDto> Items { get; set; } = new();
    }
}
