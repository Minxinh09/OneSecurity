namespace OneSecurity.Collector.Configuration
{
    public class CollectorOptions
    {
        public int BatchSize { get; set; } = 100;
        public int FlushIntervalSeconds { get; set; } = 5;
        public int MaxRetryIntervalSeconds { get; set; } = 60;
        public string ServerBaseUrl { get; set; } = "http://localhost:5082";
        public int HeartbeatQueueSize { get; set; } = 5000;
        public int MetricQueueSize { get; set; } = 10000;
        public int SecurityEventQueueSize { get; set; } = 10000;
        public string ApiKey { get; set; } = "onesecurity_secret_key_2026";
        public long CollectorId { get; set; } = 1;
        public string CollectorSecret { get; set; } = string.Empty;
    }
}
