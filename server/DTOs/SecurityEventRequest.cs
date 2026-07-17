using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
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
    }
}
