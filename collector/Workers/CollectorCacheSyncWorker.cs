using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneSecurity.Collector.Configuration;
using OneSecurity.Collector.DTOs;
using OneSecurity.Collector.Services;

namespace OneSecurity.Collector.Workers
{
    public class CollectorCacheSyncWorker : BackgroundService
    {
        private readonly ICollectorCacheService _cacheService;
        private readonly CollectorOptions _options;
        private readonly ILogger<CollectorCacheSyncWorker> _logger;
        private readonly HttpClient _httpClient;

        public CollectorCacheSyncWorker(
            ICollectorCacheService cacheService,
            IOptions<CollectorOptions> options,
            ILogger<CollectorCacheSyncWorker> logger)
        {
            _cacheService = cacheService;
            _options = options.Value;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Collector Cache Sync Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    int currentConfigVersion = _cacheService.GetConfigVersion();
                    int currentRulesVersion = _cacheService.GetRulesVersion();

                    var requestUrl = $"{_options.ServerBaseUrl}/api/v1/collectors/{_options.CollectorId}/sync?configVersion={currentConfigVersion}&rulesVersion={currentRulesVersion}";
                    
                    using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                    request.Headers.Add("X-Collector-Secret", _options.CollectorSecret);

                    var response = await _httpClient.SendAsync(request, stoppingToken);
                    if (response.IsSuccessStatusCode)
                    {
                        var syncData = await response.Content.ReadFromJsonAsync<CollectorSyncData>(cancellationToken: stoppingToken);
                        if (syncData != null)
                        {
                            if (syncData.IsUpToDate)
                            {
                                _logger.LogDebug("Collector configuration is up-to-date (v{Version}).", currentConfigVersion);
                            }
                            else
                            {
                                _cacheService.UpdateCache(syncData);
                                _logger.LogInformation("Collector cache synchronized. New Config Version: {ConfigVersion}, Rules Version: {RulesVersion}.", 
                                    syncData.ConfigurationVersion, syncData.RulesVersion);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Collector sync failed. Status Code: {StatusCode}", response.StatusCode);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Collector Cache Sync Worker.");
                }

                // Poll every 10 seconds
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("Collector Cache Sync Worker stopped.");
        }

        public override void Dispose()
        {
            _httpClient.Dispose();
            base.Dispose();
        }
    }
}
