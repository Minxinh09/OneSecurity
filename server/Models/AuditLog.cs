using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public DateTime TimestampUtc { get; set; }

        [MaxLength(36)]
        public string? UserId { get; set; }

        [MaxLength(256)]
        public string? UserName { get; set; }

        [MaxLength(256)]
        public string? Roles { get; set; } // Renamed from Role

        [Required]
        [MaxLength(256)]
        public string Action { get; set; } = string.Empty;

        [Required]
        public AuditResourceType ResourceType { get; set; } // Enum type instead of string

        [MaxLength(256)]
        public string? EntityId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(1000)]
        public string? UserAgent { get; set; }

        public bool Success { get; set; }

        public int StatusCode { get; set; }

        [Required]
        public AuditSeverity Severity { get; set; } // Enum type instead of string

        [MaxLength(256)]
        public string? CorrelationId { get; set; }
    }
}
