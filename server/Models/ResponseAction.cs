using System;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Models
{
    public class ResponseAction
    {
        public long Id { get; set; }
        
        public long IncidentId { get; set; }
        public Incident? Incident { get; set; } // Navigation property

        public required string AgentId { get; set; }
        public Agent? Agent { get; set; } // Navigation property

        public ResponseActionType ActionType { get; set; }
        public ResponseStatus Status { get; set; } = ResponseStatus.Pending;

        public required string RequestedByUserId { get; set; }
        public ApplicationUser? RequestedByUser { get; set; } // Navigation property

        public string? ApprovedByUserId { get; set; }
        public ApplicationUser? ApprovedByUser { get; set; } // Navigation property

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public string? ResultMessage { get; set; }
        public required string CorrelationId { get; set; }
        
        public string? Metadata { get; set; } // JSON metadata
        
        public string? Parameters { get; set; } // Nullable JSON/Text parameters
        public string? ErrorMessage { get; set; } // Nullable error message

        // Checklist 23.1 additions
        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; } // Hospital navigation
        public string? CreatedBy { get; set; } // CreatedBy string
        public string? Output { get; set; } // Output string
    }
}
