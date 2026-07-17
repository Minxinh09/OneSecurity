using System;
using OneSecurity.Collector.DTOs;

namespace OneSecurity.Collector.Services
{
    public interface IEnrichmentService
    {
        EnrichedHeartbeatDto EnrichHeartbeat(HeartbeatRequest request, string correlationId, string collectorId);
        EnrichedMetricDto EnrichMetric(MetricRequest request, string correlationId, string collectorId);
        EnrichedSecurityEventDto EnrichSecurityEvent(SecurityEventRequest request, string correlationId, string collectorId);
    }

    public class EnrichmentService : IEnrichmentService
    {
        public EnrichedHeartbeatDto EnrichHeartbeat(HeartbeatRequest request, string correlationId, string collectorId)
        {
            return new EnrichedHeartbeatDto
            {
                AgentId = request.AgentId,
                CorrelationId = correlationId,
                CollectorId = collectorId,
                ReceivedTime = DateTime.UtcNow
            };
        }

        public EnrichedMetricDto EnrichMetric(MetricRequest request, string correlationId, string collectorId)
        {
            return new EnrichedMetricDto
            {
                AgentId = request.AgentId,
                CpuUsagePercent = request.CpuUsagePercent,
                RamUsagePercent = request.RamUsagePercent,
                DiskUsagePercent = request.DiskUsagePercent,
                NetworkInBytes = request.NetworkInBytes,
                NetworkOutBytes = request.NetworkOutBytes,
                CorrelationId = correlationId,
                CollectorId = collectorId,
                ReceivedTime = DateTime.UtcNow
            };
        }

        public EnrichedSecurityEventDto EnrichSecurityEvent(SecurityEventRequest request, string correlationId, string collectorId)
        {
            return new EnrichedSecurityEventDto
            {
                EventId = request.EventId,
                AgentId = request.AgentId,
                Category = request.Category,
                Severity = request.Severity,
                Source = request.Source,
                Title = request.Title,
                Details = request.Details,
                RawData = request.RawData,
                CorrelationId = correlationId,
                CollectorId = collectorId,
                ReceivedTime = DateTime.UtcNow
            };
        }
    }
}
