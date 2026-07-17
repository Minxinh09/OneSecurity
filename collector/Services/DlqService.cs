using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OneSecurity.Collector.Services
{
    public interface IDlqService
    {
        Task EnqueueDeadLetterAsync<T>(string type, T payload, string? reason = null);
    }

    public class DlqService : IDlqService
    {
        private readonly ILogger<DlqService> _logger;
        private readonly string _dlqDirectory;

        public DlqService(ILogger<DlqService> logger)
        {
            _logger = logger;
            _dlqDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deadletters");
            
            try
            {
                if (!Directory.Exists(_dlqDirectory))
                {
                    Directory.CreateDirectory(_dlqDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create DLQ directory: {Path}", _dlqDirectory);
            }
        }

        public async Task EnqueueDeadLetterAsync<T>(string type, T payload, string? reason = null)
        {
            try
            {
                if (!Directory.Exists(_dlqDirectory))
                {
                    Directory.CreateDirectory(_dlqDirectory);
                }

                string filename = $"dlq-{type.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}.json";
                string fullPath = Path.Combine(_dlqDirectory, filename);

                var wrapper = new
                {
                    Type = type,
                    DroppedAt = DateTime.UtcNow,
                    Reason = reason ?? "Queue overflow or permanent delivery failure",
                    Payload = payload
                };

                string json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(fullPath, json);
                
                _logger.LogWarning("Telemetry payload moved to Dead Letter Queue: {FileName}. Reason: {Reason}", filename, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write dead letter file for payload of type {Type}", type);
            }
        }
    }
}
