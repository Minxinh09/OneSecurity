namespace OneSecurity.Collector.DTOs
{
    public class AgentCommand
    {
        public string CommandId { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string? Metadata { get; set; }
    }
}
