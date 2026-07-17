namespace OneSecurity.Server.DTOs
{
    public class CreateResponseActionResponse
    {
        public long Id { get; set; }
        public required string AgentId { get; set; }
        public required string ActionType { get; set; }
        public required string Status { get; set; }
        public required string CorrelationId { get; set; }
    }
}
