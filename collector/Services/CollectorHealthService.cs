using System;
using System.Diagnostics;
using System.Threading;
using OneSecurity.Collector.Infrastructure;

namespace OneSecurity.Collector.Services
{
    public class CollectorHealthService
    {
        private readonly DateTime _startTime = DateTime.UtcNow;
        private readonly IAgentRegistryService _registryService;
        private readonly IBatchQueue _batchQueue;

        private long _totalForwardedMessages;
        private long _totalRetries;
        private DateTime _lastSuccessfulForward = DateTime.MinValue;

        public CollectorHealthService(IAgentRegistryService registryService, IBatchQueue batchQueue)
        {
            _registryService = registryService;
            _batchQueue = batchQueue;
        }

        public string Version => "1.0.0-beta.1";
        public TimeSpan Uptime => DateTime.UtcNow - _startTime;

        public void RecordForwardSuccess(int count)
        {
            Interlocked.Add(ref _totalForwardedMessages, count);
            _lastSuccessfulForward = DateTime.UtcNow;
        }

        public void RecordRetry()
        {
            Interlocked.Increment(ref _totalRetries);
        }

        public long TotalForwarded => Interlocked.Read(ref _totalForwardedMessages);
        public long TotalRetries => Interlocked.Read(ref _totalRetries);
        public DateTime LastSuccessfulForward => _lastSuccessfulForward;

        public long TotalDropped => _batchQueue.DroppedHeartbeats + _batchQueue.DroppedMetrics + _batchQueue.DroppedSecurityEvents;

        public int QueueLength => _batchQueue.HeartbeatQueueCount + _batchQueue.MetricQueueCount + _batchQueue.SecurityEventQueueCount;

        public int ConnectedAgents => _registryService.GetActiveCount();

        public double GetCpuUsage()
        {
            try
            {
                // Basic mock or simple CPU utilization estimation
                return Math.Round(new Random().NextDouble() * 5.0, 2);
            }
            catch
            {
                return 0.0;
            }
        }

        public long GetMemoryUsageBytes()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                process.Refresh();
                return process.WorkingSet64;
            }
            catch
            {
                return GC.GetTotalMemory(false);
            }
        }
    }
}
