using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Models
{
    [Table("incidents")]
    public class Incident
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public IncidentSeverity Severity { get; set; }

        [Required]
        public IncidentStatus Status { get; set; }

        [MaxLength(36)]
        public string? AssignedUserId { get; set; }
        
        [ForeignKey(nameof(AssignedUserId))]
        public ApplicationUser? AssignedUser { get; set; }

        public DateTime? AssignedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        [MaxLength(36)]
        public string? ResolvedByUserId { get; set; }

        public DateTime? ClosedAt { get; set; }

        [MaxLength(36)]
        public string? ClosedByUserId { get; set; }

        [MaxLength(36)]
        public string? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUser? CreatedBy { get; set; }

        public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
    }
}
