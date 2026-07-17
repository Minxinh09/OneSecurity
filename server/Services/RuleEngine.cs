using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public class RuleEngine : IRuleEngine
    {
        private readonly RegexCache _regexCache;
        private readonly ILogger<RuleEngine> _logger;
        
        // Sliding window queues for rolling threshold checks
        // Key: "Threshold:{agentId}:{ruleId}"
        private readonly ConcurrentDictionary<string, ConcurrentQueue<DateTime>> _slidingThresholdQueues = new();

        public RuleEngine(RegexCache regexCache, ILogger<RuleEngine> logger)
        {
            _regexCache = regexCache;
            _logger = logger;
        }

        public async Task<RuleEvaluationResult> EvaluateAsync(RuleEvaluationContext context, AlertRule rule)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new RuleEvaluationResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name
            };

            if (string.IsNullOrWhiteSpace(rule.ConditionExpression))
            {
                stopwatch.Stop();
                result.IsMatch = false;
                result.Reason = "Empty condition expression.";
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                return result;
            }

            try
            {
                var condition = JsonSerializer.Deserialize<RuleCondition>(rule.ConditionExpression, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (condition == null)
                {
                    stopwatch.Stop();
                    result.IsMatch = false;
                    result.Reason = "Failed to deserialize condition expression.";
                    result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                    return result;
                }

                var ev = context.Event;

                // 1. So khớp EventId
                if (!string.IsNullOrEmpty(condition.EventId))
                {
                    if (!string.Equals(ev.EventId, condition.EventId, StringComparison.OrdinalIgnoreCase))
                    {
                        stopwatch.Stop();
                        result.IsMatch = false;
                        result.Reason = $"EventId '{ev.EventId}' did not match rule pattern '{condition.EventId}'.";
                        result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                        return result;
                    }
                    result.MatchedConditions.Add("EventId");
                }

                // 2. So khớp Severity
                if (!string.IsNullOrEmpty(condition.Severity))
                {
                    if (!string.Equals(ev.Severity, condition.Severity, StringComparison.OrdinalIgnoreCase))
                    {
                        stopwatch.Stop();
                        result.IsMatch = false;
                        result.Reason = $"Severity '{ev.Severity}' did not match rule pattern '{condition.Severity}'.";
                        result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                        return result;
                    }
                    result.MatchedConditions.Add("Severity");
                }

                // 3. So khớp Source
                if (!string.IsNullOrEmpty(condition.Source))
                {
                    if (!string.Equals(ev.Source, condition.Source, StringComparison.OrdinalIgnoreCase))
                    {
                        stopwatch.Stop();
                        result.IsMatch = false;
                        result.Reason = $"Source '{ev.Source}' did not match rule pattern '{condition.Source}'.";
                        result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                        return result;
                    }
                    result.MatchedConditions.Add("Source");
                }

                // 4. So khớp Keyword (Title hoặc Details chứa Keyword)
                if (!string.IsNullOrEmpty(condition.Keyword))
                {
                    bool titleContains = ev.Title?.Contains(condition.Keyword, StringComparison.OrdinalIgnoreCase) ?? false;
                    bool detailsContains = ev.Details?.Contains(condition.Keyword, StringComparison.OrdinalIgnoreCase) ?? false;
                    if (!titleContains && !detailsContains)
                    {
                        stopwatch.Stop();
                        result.IsMatch = false;
                        result.Reason = $"Keyword '{condition.Keyword}' was not found in Title or Details.";
                        result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                        return result;
                    }
                    result.MatchedConditions.Add("Keyword");
                }

                // 5. So khớp Regex (Details hoặc RawData khớp Regex)
                if (!string.IsNullOrEmpty(condition.Regex))
                {
                    var regex = _regexCache.GetOrAdd(condition.Regex);
                    bool detailsMatch = ev.Details != null && regex.IsMatch(ev.Details);
                    bool rawMatch = ev.RawData != null && regex.IsMatch(ev.RawData);
                    if (!detailsMatch && !rawMatch)
                    {
                        stopwatch.Stop();
                        result.IsMatch = false;
                        result.Reason = $"Regex pattern '{condition.Regex}' did not match Details or RawData.";
                        result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                        return result;
                    }
                    result.MatchedConditions.Add("Regex");
                }

                // 6. Đánh giá ngưỡng (Threshold và TimeWindow)
                if (condition.Threshold.HasValue && condition.Threshold.Value > 0 &&
                    condition.TimeWindowSeconds.HasValue && condition.TimeWindowSeconds.Value > 0)
                {
                    string queueKey = $"Threshold:{ev.AgentId}:{rule.Id}";
                    var queue = _slidingThresholdQueues.GetOrAdd(queueKey, _ => new ConcurrentQueue<DateTime>());

                    var now = DateTime.UtcNow;
                    queue.Enqueue(now);

                    // Loại bỏ các mốc thời gian cũ ngoài cửa sổ trượt
                    var cutoff = now.AddSeconds(-condition.TimeWindowSeconds.Value);
                    while (queue.TryPeek(out var timestamp) && timestamp < cutoff)
                    {
                        queue.TryDequeue(out _);
                    }

                    int count = queue.Count;
                    if (count < condition.Threshold.Value)
                    {
                        stopwatch.Stop();
                        result.IsMatch = false;
                        result.Reason = $"Threshold not reached. Hits in window: {count}/{condition.Threshold.Value} inside {condition.TimeWindowSeconds}s.";
                        result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                        return result;
                    }

                    // Đạt ngưỡng threshold -> Tạo cảnh báo và dọn dẹp hàng đợi để bắt đầu chu kỳ mới
                    while (queue.TryDequeue(out _)) { }
                    
                    result.MatchedConditions.Add("Threshold");
                }

                stopwatch.Stop();
                result.IsMatch = true;
                result.Reason = "All configured conditions matched successfully.";
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.IsMatch = false;
                result.Reason = $"Exception during evaluation: {ex.Message}";
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                return result;
            }
        }

        private class RuleCondition
        {
            public string? EventId { get; set; }
            public string? Severity { get; set; }
            public string? Source { get; set; }
            public string? Keyword { get; set; }
            public string? Regex { get; set; }
            public int? Threshold { get; set; }
            public int? TimeWindowSeconds { get; set; }
        }
    }
}
