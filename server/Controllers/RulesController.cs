using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    public class RuleTestRequest
    {
        public CreateAlertRuleRequest? Rule { get; set; }
        public SecurityEventRequest? Event { get; set; }
    }

    public class RuleTestResponse
    {
        public bool Matched { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> MatchedConditions { get; set; } = new();
        public double ExecutionTimeMs { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/v1/rules")]
    public class RulesController : ControllerBase
    {
        private readonly IRuleEngine _ruleEngine;
        private readonly IRuleStatisticsTracker _statsTracker;
        private readonly IRuleCacheService _ruleCacheService;

        public RulesController(
            IRuleEngine ruleEngine,
            IRuleStatisticsTracker statsTracker,
            IRuleCacheService ruleCacheService)
        {
            _ruleEngine = ruleEngine;
            _statsTracker = statsTracker;
            _ruleCacheService = ruleCacheService;
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            var activeRules = await _ruleCacheService.GetActiveRulesAsync();
            var stats = _statsTracker.GetStatistics(activeRules.Count);
            return Ok(stats);
        }

        [HttpPost("test")]
        public async Task<IActionResult> TestRule([FromBody] RuleTestRequest request)
        {
            if (request?.Rule == null || request?.Event == null)
            {
                return BadRequest(new { message = "Both rule and event must be provided in the request body." });
            }

            // Create temporary AlertRule
            var tempRule = new AlertRule
            {
                Id = 9999, // Dummy ID for sandbox testing
                Name = request.Rule.Name ?? "Test Rule",
                EventType = request.Rule.EventType ?? "security",
                ConditionExpression = request.Rule.ConditionExpression,
                AlertSeverity = request.Rule.AlertSeverity ?? "Warning",
                IsEnabled = true
            };

            // Create temporary SecurityEvent
            var tempEvent = new SecurityEvent
            {
                EventId = request.Event.EventId ?? Guid.NewGuid().ToString(),
                AgentId = request.Event.AgentId ?? "test-agent",
                Category = request.Event.Category ?? "security",
                Severity = request.Event.Severity ?? "warning",
                Source = request.Event.Source ?? "test-source",
                Title = request.Event.Title ?? "Test Event",
                Details = request.Event.Details ?? "",
                RawData = request.Event.RawData ?? "{}"
            };

            var context = new RuleEvaluationContext
            {
                Event = tempEvent,
                Agent = new Agent 
                { 
                    Id = tempEvent.AgentId,
                    Hostname = "Test-Host",
                    IpAddress = "127.0.0.1",
                    OsInfo = "Windows",
                    Status = "online"
                }
            };

            var result = await _ruleEngine.EvaluateAsync(context, tempRule);

            return Ok(new RuleTestResponse
            {
                Matched = result.IsMatch,
                Reason = result.Reason ?? string.Empty,
                MatchedConditions = result.MatchedConditions,
                ExecutionTimeMs = result.ExecutionTimeMs
            });
        }
    }
}
