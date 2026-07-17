using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OneSecurity.Server.DTOs
{
    public class BulkAcknowledgeRequest
    {
        [Required(ErrorMessage = "AlertIds are required")]
        [MinLength(1, ErrorMessage = "At least one AlertId must be provided")]
        public required List<long> AlertIds { get; set; }
    }
}
