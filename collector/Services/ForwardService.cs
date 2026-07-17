using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneSecurity.Collector.Configuration;
using OneSecurity.Collector.DTOs;

namespace OneSecurity.Collector.Services
{
    public class ForwardRegisterResult
    {
        public bool IsSuccess { get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; } = System.Net.HttpStatusCode.BadGateway;
        public RegisterAgentResponse? Response { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IForwardService
    {
        Task<bool> ForwardHeartbeatsAsync(List<EnrichedHeartbeatDto> batch, CancellationToken cancellationToken);
        Task<bool> ForwardMetricsAsync(List<EnrichedMetricDto> batch, CancellationToken cancellationToken);
        Task<bool> ForwardSecurityEventsAsync(List<EnrichedSecurityEventDto> batch, CancellationToken cancellationToken);
        Task<ForwardRegisterResult> ForwardRegisterAsync(RegisterAgentRequest request, CancellationToken cancellationToken);
    }

    public class ForwardService : IForwardService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ForwardService> _logger;
        private readonly CollectorOptions _options;
        private readonly CollectorHealthService _healthService;

        public ForwardService(
            HttpClient httpClient, 
            ILogger<ForwardService> logger, 
            IOptions<CollectorOptions> options,
            CollectorHealthService healthService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
            _healthService = healthService;
            
            _httpClient.BaseAddress = new Uri(_options.ServerBaseUrl);
        }

        public async Task<bool> ForwardHeartbeatsAsync(List<EnrichedHeartbeatDto> batch, CancellationToken cancellationToken)
        {
            return await ForwardBatchWithRetryAsync("/api/v1/agent/heartbeat/batch", batch, cancellationToken);
        }

        public async Task<bool> ForwardMetricsAsync(List<EnrichedMetricDto> batch, CancellationToken cancellationToken)
        {
            return await ForwardBatchWithRetryAsync("/api/v1/agent/metrics/batch", batch, cancellationToken);
        }

        public async Task<bool> ForwardSecurityEventsAsync(List<EnrichedSecurityEventDto> batch, CancellationToken cancellationToken)
        {
            return await ForwardBatchWithRetryAsync("/api/v1/agent/events/batch", batch, cancellationToken);
        }

        public async Task<ForwardRegisterResult> ForwardRegisterAsync(RegisterAgentRequest request, CancellationToken cancellationToken)
        {
            var result = new ForwardRegisterResult();
            try
            {
                var content = CreateJsonContent(request);
                var response = await _httpClient.PostAsync("/api/v1/agent/register", content, cancellationToken);
                result.StatusCode = response.StatusCode;
                
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    result.IsSuccess = true;
                    result.Response = JsonSerializer.Deserialize<RegisterAgentResponse>(responseBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = responseBody;
                    _logger.LogError("Failed to forward agent registration. Server status: {Status}, Message: {Msg}", response.StatusCode, responseBody);
                }
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Exception occurred while forwarding agent registration.");
            }
            return result;
        }

        private async Task<bool> ForwardBatchWithRetryAsync<T>(string path, List<T> batch, CancellationToken cancellationToken)
        {
            if (batch == null || batch.Count == 0) return true;

            int retryDelaySeconds = 1;
            int attempt = 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Forwarding batch of {Count} items to {Path} (Attempt {Attempt})...", batch.Count, path, attempt);
                    
                    var content = CreateCompressedJsonContent(batch);
                    var response = await _httpClient.PostAsync(path, content, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Successfully forwarded batch of {Count} items to {Path}.", batch.Count, path);
                        _healthService.RecordForwardSuccess(batch.Count);
                        return true;
                    }

                    _logger.LogWarning("Server returned error status code: {StatusCode} for {Path}.", response.StatusCode, path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while forwarding batch to {Path}.", path);
                }

                _healthService.RecordRetry();

                int delayMs = retryDelaySeconds * 1000;
                _logger.LogInformation("Waiting {Delay}s before retrying...", retryDelaySeconds);
                
                try
                {
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                attempt++;
                retryDelaySeconds = Math.Min(retryDelaySeconds * 2, _options.MaxRetryIntervalSeconds);
            }

            return false;
        }

        private HttpContent CreateJsonContent<T>(T payload)
        {
            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return content;
        }

        private HttpContent CreateCompressedJsonContent<T>(T payload)
        {
            string json = JsonSerializer.Serialize(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            var ms = new MemoryStream();
            using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }
            ms.Position = 0;

            var content = new ByteArrayContent(ms.ToArray());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Headers.ContentEncoding.Add("gzip");
            return content;
        }
    }
}
