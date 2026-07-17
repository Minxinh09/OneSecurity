using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OneSecurity.Server.Services
{
    public class RuleStatisticsTracker : IRuleStatisticsTracker
    {
        private long _totalEvaluations = 0;
        private long _totalMatches = 0;
        private double _totalElapsedMs = 0;
        private string? _lastEvaluationTime;
        private readonly object _lock = new();

        private readonly ConcurrentDictionary<long, (string Name, long Count)> _ruleMatches = new();
        private readonly ConcurrentDictionary<long, (string Name, List<double> Times)> _ruleTimes = new();
        private readonly ConcurrentDictionary<long, (string Name, long Failures)> _ruleFailures = new();

        public void RecordEvaluation(long ruleId, string ruleName, bool isMatch, bool isFailed, double elapsedMilliseconds)
        {
            Interlocked.Increment(ref _totalEvaluations);
            
            if (isMatch)
            {
                Interlocked.Increment(ref _totalMatches);
                _ruleMatches.AddOrUpdate(ruleId, 
                    (ruleName, 1), 
                    (key, old) => (ruleName, old.Count + 1));
            }

            if (isFailed)
            {
                _ruleFailures.AddOrUpdate(ruleId, 
                    (ruleName, 1), 
                    (key, old) => (ruleName, old.Failures + 1));
            }

            _ruleTimes.AddOrUpdate(ruleId,
                (ruleName, new List<double> { elapsedMilliseconds }),
                (key, old) =>
                {
                    lock (old.Times)
                    {
                        old.Times.Add(elapsedMilliseconds);
                        if (old.Times.Count > 1000)
                        {
                            old.Times.RemoveAt(0);
                        }
                    }
                    return old;
                });

            lock (_lock)
            {
                _totalElapsedMs += elapsedMilliseconds;
                _lastEvaluationTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
            }
        }

        public RuleStatisticsDto GetStatistics(int activeRulesCount)
        {
            var stats = new RuleStatisticsDto
            {
                TotalEvaluations = Interlocked.Read(ref _totalEvaluations),
                TotalMatches = Interlocked.Read(ref _totalMatches),
                CurrentActiveRulesCount = activeRulesCount
            };

            lock (_lock)
            {
                stats.AverageEvaluationTimeMs = stats.TotalEvaluations > 0 
                    ? _totalElapsedMs / stats.TotalEvaluations 
                    : 0;
                stats.LastEvaluationTime = _lastEvaluationTime;
            }

            stats.RuleHitRate = stats.TotalEvaluations > 0 
                ? ((double)stats.TotalMatches / stats.TotalEvaluations) * 100 
                : 0;

            stats.TopSlowRules = _ruleTimes
                .Select(kv =>
                {
                    double avg = 0;
                    lock (kv.Value.Times)
                    {
                        avg = kv.Value.Times.Count > 0 ? kv.Value.Times.Average() : 0;
                    }
                    return new RulePerformanceStatDto
                    {
                        RuleId = kv.Key,
                        RuleName = kv.Value.Name,
                        Value = avg
                    };
                })
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToList();

            stats.TopFailedRules = _ruleFailures
                .Select(kv => new RulePerformanceStatDto
                {
                    RuleId = kv.Key,
                    RuleName = kv.Value.Name,
                    Value = kv.Value.Failures
                })
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToList();

            stats.TopTriggeredRules = _ruleMatches
                .Select(kv => new RuleMatchStatDto
                {
                    RuleId = kv.Key,
                    RuleName = kv.Value.Name,
                    MatchCount = kv.Value.Count
                })
                .OrderByDescending(x => x.MatchCount)
                .Take(5)
                .ToList();

            return stats;
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _totalEvaluations, 0);
            Interlocked.Exchange(ref _totalMatches, 0);
            lock (_lock)
            {
                _totalElapsedMs = 0;
                _lastEvaluationTime = null;
            }
            _ruleMatches.Clear();
            _ruleTimes.Clear();
            _ruleFailures.Clear();
        }
    }
}
