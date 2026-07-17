using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
    public class HeartbeatRequest
    {
        [Required(ErrorMessage = "AgentId is required")]
        [StringLength(36)]
        public required string AgentId { get; set; }
    }
}
