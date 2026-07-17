using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public class RuleEvaluationContext
    {
        public required SecurityEvent Event { get; set; }
        public required Agent Agent { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
    }
}
