using System;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Collector.Services;

namespace OneSecurity.Collector.Controllers
{
    [ApiController]
    [Route("metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly CollectorHealthService _healthService;

        public MetricsController(CollectorHealthService healthService)
        {
            _healthService = healthService;
        }

        [HttpGet]
        public IActionResult GetMetrics()
        {
            var sb = new StringBuilder();

            sb.AppendLine("# HELP onesecurity_collector_uptime_seconds The uptime of the collector in seconds.");
            sb.AppendLine("# TYPE onesecurity_collector_uptime_seconds counter");
            sb.AppendLine($"onesecurity_collector_uptime_seconds {_healthService.Uptime.TotalSeconds}");

            sb.AppendLine("# HELP onesecurity_collector_connected_agents Current active connected agents count.");
            sb.AppendLine("# TYPE onesecurity_collector_connected_agents gauge");
            sb.AppendLine($"onesecurity_collector_connected_agents {_healthService.ConnectedAgents}");

            sb.AppendLine("# HELP onesecurity_collector_queue_length Combined queue length of heartbeats, metrics, and events.");
            sb.AppendLine("# TYPE onesecurity_collector_queue_length gauge");
            sb.AppendLine($"onesecurity_collector_queue_length {_healthService.QueueLength}");

            sb.AppendLine("# HELP onesecurity_collector_dropped_messages_total Cumulative count of dropped telemetry packets.");
            sb.AppendLine("# TYPE onesecurity_collector_dropped_messages_total counter");
            sb.AppendLine($"onesecurity_collector_dropped_messages_total {_healthService.TotalDropped}");

            sb.AppendLine("# HELP onesecurity_collector_retry_count_total Total batch forward retries due to server unavailability.");
            sb.AppendLine("# TYPE onesecurity_collector_retry_count_total counter");
            sb.AppendLine($"onesecurity_collector_retry_count_total {_healthService.TotalRetries}");

            sb.AppendLine("# HELP onesecurity_collector_forwarded_messages_total Total count of telemetry records successfully forwarded to the central server.");
            sb.AppendLine("# TYPE onesecurity_collector_forwarded_messages_total counter");
            sb.AppendLine($"onesecurity_collector_forwarded_messages_total {_healthService.TotalForwarded}");

            sb.AppendLine("# HELP onesecurity_collector_memory_usage_bytes Current working set size in bytes.");
            sb.AppendLine("# TYPE onesecurity_collector_memory_usage_bytes gauge");
            sb.AppendLine($"onesecurity_collector_memory_usage_bytes {_healthService.GetMemoryUsageBytes()}");

            return Content(sb.ToString(), "text/plain; version=0.0.4; charset=utf-8");
        }
    }
}
