using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OneSecurity.Server.Models
{
    public class Alert
    {
        public long Id { get; set; }
        public required string AgentId { get; set; }
        public Agent? Agent { get; set; } // Navigation Property
        public long? RuleId { get; set; }
        public AlertRule? Rule { get; set; } // Navigation Property
        public long? TriggerEventId { get; set; }
        public SecurityEvent? TriggerEvent { get; set; } // Navigation Property
        public required string RuleName { get; set; }
        public required string Severity { get; set; } // critical, warning
        public required string Title { get; set; }
        public required string Message { get; set; }
        public required string Category { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public bool TelegramSent { get; set; }

        public long? IncidentId { get; set; }
        
        [ForeignKey(nameof(IncidentId))]
        public Incident? Incident { get; set; }
    }
}
