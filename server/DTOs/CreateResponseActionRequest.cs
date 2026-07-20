using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.DTOs
{
    public class CreateResponseActionRequest
    {
        [Required(ErrorMessage = "IncidentId is required")]
        public long IncidentId { get; set; }
        
        [Required(ErrorMessage = "AgentId is required")]
        public required string AgentId { get; set; }
        
        [Required(ErrorMessage = "ActionType is required")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ResponseActionType ActionType { get; set; }
        
        public string? Parameters { get; set; }
        public string? Metadata { get; set; }
    }
}
