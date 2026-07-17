using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
    public class MetricRequest
    {
        [Required(ErrorMessage = "AgentId is required")]
        [StringLength(36)]
        public required string AgentId { get; set; }

        [Range(0, 100, ErrorMessage = "CpuUsagePercent must be between 0 and 100")]
        public decimal CpuUsagePercent { get; set; }

        [Range(0, 100, ErrorMessage = "RamUsagePercent must be between 0 and 100")]
        public decimal RamUsagePercent { get; set; }

        [Range(0, 100, ErrorMessage = "DiskUsagePercent must be between 0 and 100")]
        public decimal DiskUsagePercent { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "NetworkInBytes must be a positive number")]
        public long NetworkInBytes { get; set; }

        [Range(0, long.MaxValue, ErrorMessage = "NetworkOutBytes must be a positive number")]
        public long NetworkOutBytes { get; set; }
    }
}
