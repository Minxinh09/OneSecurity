using OneSecurity.Collector.DTOs;

namespace OneSecurity.Collector.Services
{
    public interface INormalizationService
    {
        HeartbeatRequest NormalizeHeartbeat(HeartbeatRequest request);
        MetricRequest NormalizeMetric(MetricRequest request);
        SecurityEventRequest NormalizeSecurityEvent(SecurityEventRequest request);
    }

    public class NormalizationService : INormalizationService
    {
        public HeartbeatRequest NormalizeHeartbeat(HeartbeatRequest request)
        {
            request.AgentId = request.AgentId.Trim().ToLowerInvariant();
            return request;
        }

        public MetricRequest NormalizeMetric(MetricRequest request)
        {
            request.AgentId = request.AgentId.Trim().ToLowerInvariant();
            return request;
        }

        public SecurityEventRequest NormalizeSecurityEvent(SecurityEventRequest request)
        {
            request.AgentId = request.AgentId.Trim().ToLowerInvariant();
            request.EventId = request.EventId.Trim();
            
            if (!string.IsNullOrWhiteSpace(request.Severity))
            {
                string s = request.Severity.Trim().ToLowerInvariant();
                request.Severity = s switch
                {
                    "critical" => "Critical",
                    "warning" => "Warning",
                    "info" => "Info",
                    _ => request.Severity
                };
            }

            request.Category = request.Category?.Trim() ?? "Unknown";
            request.Source = request.Source?.Trim() ?? "Collector";
            request.Title = request.Title?.Trim() ?? "Security Log Event";

            return request;
        }
    }
}
