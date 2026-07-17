using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
    public class RegisterAgentRequest
    {
        [Required(ErrorMessage = "Hostname is required")]
        [StringLength(255)]
        public required string Hostname { get; set; }

        [Required(ErrorMessage = "IP Address is required")]
        [StringLength(45)]
        public required string IpAddress { get; set; }

        [Required(ErrorMessage = "OS Info is required")]
        [StringLength(255)]
        public required string OsInfo { get; set; }

        [StringLength(50)]
        public string? HospitalCode { get; set; }

        public string? SupportedActions { get; set; }
        public string? Capabilities { get; set; }
        public string? AgentVersion { get; set; }
        public string? CollectorVersion { get; set; }
        public string? EnrollmentToken { get; set; }
        public long CollectorId { get; set; }
    }
}
