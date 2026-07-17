using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
    public class UpdateAlertRuleRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, ErrorMessage = "Name must not exceed 100 characters")]
        public required string Name { get; set; }

        [StringLength(100, ErrorMessage = "EventType must not exceed 100 characters")]
        public string? EventType { get; set; }

        [Required(ErrorMessage = "ConditionExpression is required")]
        public required string ConditionExpression { get; set; } // JSON String

        [Required(ErrorMessage = "AlertSeverity is required")]
        [StringLength(20, ErrorMessage = "AlertSeverity must not exceed 20 characters")]
        public required string AlertSeverity { get; set; }

        public bool IsEnabled { get; set; }

        [StringLength(50, ErrorMessage = "TelegramChatId must not exceed 50 characters")]
        public string? TelegramChatId { get; set; }

        public int Priority { get; set; } = 3;
        public string Category { get; set; } = "General";
        public int Version { get; set; } = 1;
    }
}
