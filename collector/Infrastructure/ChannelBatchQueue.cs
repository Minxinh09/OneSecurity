using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using OneSecurity.Collector.Configuration;
using OneSecurity.Collector.DTOs;

namespace OneSecurity.Collector.Infrastructure
{
    public interface IBatchQueue
    {
        bool TryEnqueueHeartbeat(EnrichedHeartbeatDto item);
        bool TryEnqueueMetric(EnrichedMetricDto item);
        bool TryEnqueueSecurityEvent(EnrichedSecurityEventDto item);

        Task<List<EnrichedHeartbeatDto>> DequeueHeartbeatBatchAsync(int maxBatchSize, TimeSpan maxWaitTime, CancellationToken cancellationToken);
        Task<List<EnrichedMetricDto>> DequeueMetricBatchAsync(int maxBatchSize, TimeSpan maxWaitTime, CancellationToken cancellationToken);
        Task<List<EnrichedSecurityEventDto>> DequeueSecurityEventBatchAsync(int maxBatchSize, TimeSpan maxWaitTime, CancellationToken cancellationToken);

        int HeartbeatQueueCount { get; }
        int MetricQueueCount { get; }
        int SecurityEventQueueCount { get; }

        long DroppedHeartbeats { get; }
        long DroppedMetrics { get; }
        long DroppedSecurityEvents { get; }
    }

    public class ChannelBatchQueue : IBatchQueue
    {
        private readonly Channel<EnrichedHeartbeatDto> _heartbeatChannel;
        private readonly Channel<EnrichedMetricDto> _metricChannel;
        private readonly Channel<EnrichedSecurityEventDto> _eventChannel;

        private long _droppedHeartbeats;
        private long _droppedMetrics;
        private long _droppedEvents;

        public ChannelBatchQueue(IOptions<CollectorOptions> options)
        {
            var opt = options.Value;

            _heartbeatChannel = Channel.CreateBounded<EnrichedHeartbeatDto>(new BoundedChannelOptions(opt.HeartbeatQueueSize)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });

            _metricChannel = Channel.CreateBounded<EnrichedMetricDto>(new BoundedChannelOptions(opt.MetricQueueSize)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });

            _eventChannel = Channel.CreateBounded<EnrichedSecurityEventDto>(new BoundedChannelOptions(opt.SecurityEventQueueSize)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });
        }

        public bool TryEnqueueHeartbeat(EnrichedHeartbeatDto item)
        {
            if (_heartbeatChannel.Writer.TryWrite(item))
            {
                return true;
            }
            Interlocked.Increment(ref _droppedHeartbeats);
            return false;
        }

        public bool TryEnqueueMetric(EnrichedMetricDto item)
        {
            if (_metricChannel.Writer.TryWrite(item))
            {
                return true;
            }
            Interlocked.Increment(ref _droppedMetrics);
            return false;
        }

        public bool TryEnqueueSecurityEvent(EnrichedSecurityEventDto item)
        {
            if (_eventChannel.Writer.TryWrite(item))
            {
                return true;
            }
            Interlocked.Increment(ref _droppedEvents);
            return false;
        }

        public Task<List<EnrichedHeartbeatDto>> DequeueHeartbeatBatchAsync(int maxBatchSize, TimeSpan maxWaitTime, CancellationToken cancellationToken)
        {
            return DequeueBatchAsync(_heartbeatChannel.Reader, maxBatchSize, maxWaitTime, cancellationToken);
        }

        public Task<List<EnrichedMetricDto>> DequeueMetricBatchAsync(int maxBatchSize, TimeSpan maxWaitTime, CancellationToken cancellationToken)
        {
            return DequeueBatchAsync(_metricChannel.Reader, maxBatchSize, maxWaitTime, cancellationToken);
        }

        public Task<List<EnrichedSecurityEventDto>> DequeueSecurityEventBatchAsync(int maxBatchSize, TimeSpan maxWaitTime, CancellationToken cancellationToken)
        {
            return DequeueBatchAsync(_eventChannel.Reader, maxBatchSize, maxWaitTime, cancellationToken);
        }

        public int HeartbeatQueueCount => _heartbeatChannel.Reader.Count;
        public int MetricQueueCount => _metricChannel.Reader.Count;
        public int SecurityEventQueueCount => _eventChannel.Reader.Count;

        public long DroppedHeartbeats => Interlocked.Read(ref _droppedHeartbeats);
        public long DroppedMetrics => Interlocked.Read(ref _droppedMetrics);
        public long DroppedSecurityEvents => Interlocked.Read(ref _droppedEvents);

        private async Task<List<T>> DequeueBatchAsync<T>(
            ChannelReader<T> reader, 
            int maxBatchSize, 
            TimeSpan maxWaitTime, 
            CancellationToken cancellationToken)
        {
            var list = new List<T>();
            using var timeoutCts = new CancellationTokenSource(maxWaitTime);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                while (list.Count < maxBatchSize)
                {
                    if (list.Count == 0)
                    {
                        if (await reader.WaitToReadAsync(linkedCts.Token))
                        {
                            if (reader.TryRead(out var item))
                            {
                                list.Add(item);
                            }
                        }
                    }
                    else
                    {
                        if (reader.TryRead(out var item))
                        {
                            list.Add(item);
                        }
                        else
                        {
                            break; 
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                // Gracefully catch timeout to return whatever we read
            }

            return list;
        }
    }
}
