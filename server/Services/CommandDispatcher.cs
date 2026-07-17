using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CommandDispatcher> _logger;

        public CommandDispatcher(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<CommandDispatcher> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> DispatchAsync(ResponseAction action)
        {
            var collectorBaseUrl = _configuration["Collector:Url"] ?? "http://localhost:5050";
            var apiKey = _configuration["Collector:ApiKey"] ?? "onesecurity_secret_key_2026";

            var endpoint = $"{collectorBaseUrl.TrimEnd('/')}/api/v1/collector/commands";

            var commandDto = new AgentCommandDto
            {
                CommandId = action.CorrelationId,
                AgentId = action.AgentId,
                ActionType = action.ActionType.ToString(),
                Metadata = action.Metadata
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
                request.Headers.Add("X-Collector-API-Key", apiKey);
                
                var json = JsonSerializer.Serialize(commandDto);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Dispatching command {CorrelationId} ({ActionType}) to Collector: {Url}", 
                    action.CorrelationId, action.ActionType, endpoint);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully dispatched command {CorrelationId} to Collector.", action.CorrelationId);
                    return true;
                }

                var errorMsg = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to dispatch command to Collector. Status: {StatusCode}, Details: {Details}", 
                    response.StatusCode, errorMsg);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while dispatching command {CorrelationId} to Collector.", action.CorrelationId);
                return false;
            }
        }
    }
}
