using System.Collections.Generic;

namespace OneSecurity.Server.Services
{
    public class RuleEvaluationResult
    {
        public bool IsMatch { get; set; }
        public string? Reason { get; set; }
        public List<string> MatchedConditions { get; set; } = new();
        public double ExecutionTimeMs { get; set; }
        public long RuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
    }
}
