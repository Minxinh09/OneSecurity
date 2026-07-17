using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
    public class CreateResponseActionRequest
    {
        [Required(ErrorMessage = "IncidentId is required")]
        public long IncidentId { get; set; }
        
        [Required(ErrorMessage = "AgentId is required")]
        public required string AgentId { get; set; }
        
        [Required(ErrorMessage = "ActionType is required")]
        public required string ActionType { get; set; } // enum string
        
        public string? Parameters { get; set; }
        public string? Metadata { get; set; }
    }
}
