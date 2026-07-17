using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public class RuleEngineService : IRuleEngineService
    {
        private readonly IRuleCacheService _ruleCacheService;
        private readonly IRuleEngine _ruleEngine;
        private readonly IRuleStatisticsTracker _statsTracker;

        public RuleEngineService(
            IRuleCacheService ruleCacheService,
            IRuleEngine ruleEngine,
            IRuleStatisticsTracker statsTracker)
        {
            _ruleCacheService = ruleCacheService;
            _ruleEngine = ruleEngine;
            _statsTracker = statsTracker;
        }

        public async Task<List<Alert>> EvaluateEventAsync(SecurityEvent securityEvent)
        {
            var matchedAlerts = new List<Alert>();

            // 1. Lấy danh sách luật từ cache (đã được sắp xếp theo Priority)
            var activeRules = await _ruleCacheService.GetActiveRulesAsync();

            // 2. Tạo context đánh giá
            var context = new RuleEvaluationContext
            {
                Event = securityEvent,
                Agent = securityEvent.Agent
            };

            foreach (var rule in activeRules)
            {
                // 3. Đánh giá qua RuleEngine v2
                var result = await _ruleEngine.EvaluateAsync(context, rule);

                // Kiểm tra lỗi (exception)
                bool isFailed = result.Reason != null && result.Reason.StartsWith("Exception", StringComparison.OrdinalIgnoreCase);

                // 4. Lưu thống kê hiệu năng rule
                _statsTracker.RecordEvaluation(rule.Id, rule.Name, result.IsMatch, isFailed, result.ExecutionTimeMs);

                if (result.IsMatch)
                {
                    // 5. Khởi tạo đối tượng Alert khi khớp luật
                    var alert = new Alert
                    {
                        AgentId = securityEvent.AgentId,
                        Agent = securityEvent.Agent,
                        RuleId = rule.Id,
                        Rule = rule,
                        TriggerEvent = securityEvent,
                        RuleName = rule.Name,
                        Severity = rule.AlertSeverity,
                        Title = $"[Cảnh báo] {rule.Name}",
                        Message = $"Sự kiện an ninh khớp luật '{rule.Name}'. Tiêu đề: {securityEvent.Title}. Chi tiết: {securityEvent.Details}",
                        Category = rule.Category ?? securityEvent.Category,
                        CreatedAt = DateTime.UtcNow,
                        IsAcknowledged = false,
                        TelegramSent = false
                    };

                    matchedAlerts.Add(alert);
                }
            }

            return matchedAlerts;
        }
    }
}
