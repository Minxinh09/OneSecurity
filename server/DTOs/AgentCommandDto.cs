namespace OneSecurity.Server.DTOs
{
    public class AgentCommandDto
    {
        public string CommandId { get; set; } = string.Empty; // Map to CorrelationId
        public string AgentId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string? Metadata { get; set; }
    }
}
