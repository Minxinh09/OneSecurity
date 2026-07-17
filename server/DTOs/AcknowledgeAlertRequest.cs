using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
    public class AcknowledgeAlertRequest
    {
        [Required(ErrorMessage = "AlertId is required")]
        public long AlertId { get; set; }
    }
}
