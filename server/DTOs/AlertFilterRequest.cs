using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
    public class AlertFilterRequest
    {
        public string? Severity { get; set; }
        public string? Category { get; set; }
        public string? AgentId { get; set; }
        public bool? IsAcknowledged { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than or equal to 1")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;
    }
}
