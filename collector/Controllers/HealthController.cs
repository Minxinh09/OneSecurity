using System;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Collector.Services;

namespace OneSecurity.Collector.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly CollectorHealthService _healthService;

        public HealthController(CollectorHealthService healthService)
        {
            _healthService = healthService;
        }

        [HttpGet]
        public IActionResult GetHealth()
        {
            double uptimeSeconds = _healthService.Uptime.TotalSeconds;
            double avgBatchSize = _healthService.TotalForwarded > 0 
                ? Math.Round((double)_healthService.TotalForwarded / Math.Max(1, (_healthService.TotalForwarded / 100)), 2)
                : 0.0;

            var stats = new
            {
                Status = "Healthy",
                Version = _healthService.Version,
                UptimeSeconds = uptimeSeconds,
                ConnectedAgents = _healthService.ConnectedAgents,
                QueueLength = _healthService.QueueLength,
                DroppedMessages = _healthService.TotalDropped,
                RetryCount = _healthService.TotalRetries,
                LastSuccessfulForward = _healthService.LastSuccessfulForward == DateTime.MinValue 
                    ? (object)"Never" 
                    : _healthService.LastSuccessfulForward,
                AverageBatchSize = avgBatchSize,
                CpuUsagePercent = _healthService.GetCpuUsage(),
                MemoryUsageMB = Math.Round((double)_healthService.GetMemoryUsageBytes() / (1024 * 1024), 2)
            };

            return Ok(stats);
        }
    }
}
