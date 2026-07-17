using System.Collections.Generic;

namespace OneSecurity.Server.Services
{
    public class RuleStatisticsDto
    {
        public long TotalEvaluations { get; set; }
        public long TotalMatches { get; set; }
        public double AverageEvaluationTimeMs { get; set; }
        public int CurrentActiveRulesCount { get; set; }
        public string? LastEvaluationTime { get; set; }
        public double RuleHitRate { get; set; }
        public List<RulePerformanceStatDto> TopSlowRules { get; set; } = new();
        public List<RulePerformanceStatDto> TopFailedRules { get; set; } = new();
        public List<RuleMatchStatDto> TopTriggeredRules { get; set; } = new();
    }

    public class RulePerformanceStatDto
    {
        public long RuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class RuleMatchStatDto
    {
        public long RuleId { get; set; }
        public string RuleName { get; set; } = string.Empty;
        public long MatchCount { get; set; }
    }

    public interface IRuleStatisticsTracker
    {
        void RecordEvaluation(long ruleId, string ruleName, bool isMatch, bool isFailed, double elapsedMilliseconds);
        RuleStatisticsDto GetStatistics(int activeRulesCount);
        void Reset();
    }
}
