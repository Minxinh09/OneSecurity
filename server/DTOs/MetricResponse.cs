using System;

namespace OneSecurity.Server.DTOs
{
    public class MetricResponse
    {
        public long MetricRecordId { get; set; }
        public required string AgentId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
