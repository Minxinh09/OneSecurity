using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneSecurity.Collector.Configuration;
using OneSecurity.Collector.Infrastructure;
using OneSecurity.Collector.Services;

namespace OneSecurity.Collector.Workers
{
    public class HeartbeatForwardWorker : BackgroundService
    {
        private readonly IBatchQueue _queue;
        private readonly IForwardService _forwardService;
        private readonly IDlqService _dlqService;
        private readonly CollectorOptions _options;
        private readonly ILogger<HeartbeatForwardWorker> _logger;

        public HeartbeatForwardWorker(
            IBatchQueue queue,
            IForwardService forwardService,
            IDlqService dlqService,
            IOptions<CollectorOptions> options,
            ILogger<HeartbeatForwardWorker> logger)
        {
            _queue = queue;
            _forwardService = forwardService;
            _dlqService = dlqService;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Heartbeat Forward Worker started.");

            var flushInterval = TimeSpan.FromSeconds(_options.FlushIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var batch = await _queue.DequeueHeartbeatBatchAsync(_options.BatchSize, flushInterval, stoppingToken);
                    if (batch.Count > 0)
                    {
                        bool success = await _forwardService.ForwardHeartbeatsAsync(batch, stoppingToken);
                        if (!success)
                        {
                            _logger.LogWarning("Failed to forward heartbeat batch. Saving to DLQ.");
                            foreach (var item in batch)
                            {
                                await _dlqService.EnqueueDeadLetterAsync("Heartbeat", item, "Forwarding failed or cancelled");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Heartbeat Forward Worker.");
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogInformation("Heartbeat Forward Worker stopped.");
        }
    }
}
