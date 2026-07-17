using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneSecurity.Server.Configuration;
using OneSecurity.Server.Models;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class TelegramNotificationService : INotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TelegramNotificationService> _logger;
        private readonly TelegramOptions _options;
        private readonly IAlertRepository _alertRepository;

        public TelegramNotificationService(
            IHttpClientFactory httpClientFactory,
            ILogger<TelegramNotificationService> logger,
            IOptions<TelegramOptions> options,
            IAlertRepository alertRepository)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _options = options.Value;
            _alertRepository = alertRepository;
        }

        public async Task SendAlertAsync(Alert alert)
        {
            if (alert.TelegramSent)
            {
                return;
            }

            if (alert.Rule != null && !alert.Rule.IsEnabled)
            {
                return;
            }

            string? chatId = null;

            try
            {
                chatId = !string.IsNullOrWhiteSpace(alert.Rule?.TelegramChatId)
                    ? alert.Rule.TelegramChatId
                    : _options.DefaultChatId;

                if (string.IsNullOrWhiteSpace(_options.BotToken) || string.IsNullOrWhiteSpace(chatId))
                {
                    _logger.LogWarning("Telegram send skipped: BotToken or ChatId is missing. AlertId: {AlertId}, RuleName: {RuleName}", alert.Id, alert.RuleName);
                    return;
                }

                var messageText = BuildMessage(alert);

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var url = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
                var payload = new
                {
                    chat_id = chatId,
                    text = messageText,
                    parse_mode = "Markdown"
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                bool isSuccess = false;
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean())
                        {
                            isSuccess = true;
                        }
                    }
                    catch (JsonException)
                    {
                        // JSON parsing failure
                    }
                }

                if (isSuccess)
                {
                    alert.TelegramSent = true;
                    _alertRepository.Update(alert);
                    await _alertRepository.SaveChangesAsync();
                    _logger.LogInformation("Successfully sent alert {AlertId} to Telegram chat {ChatId}.", alert.Id, chatId);
                }
                else
                {
                    _logger.LogWarning("Telegram API returned failure. AlertId: {AlertId}, RuleName: {RuleName}, ChatId: {ChatId}, StatusCode: {StatusCode}, ResponseBody: {ResponseBody}, Exception: {Exception}",
                        alert.Id, alert.RuleName, chatId, (int)response.StatusCode, responseContent, "None");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send Telegram notification. AlertId: {AlertId}, RuleName: {RuleName}, ChatId: {ChatId}, StatusCode: {StatusCode}, ResponseBody: {ResponseBody}, Exception: {Exception}",
                    alert.Id, alert.RuleName, chatId ?? "Unknown", 0, "N/A", ex.Message);
            }
        }

        private string BuildMessage(Alert alert)
        {
            var hostname = alert.Agent?.Hostname ?? "Unknown";
            var sb = new StringBuilder();
            sb.AppendLine("🚨 **OneSecurity Alert**");
            sb.AppendLine();
            sb.AppendLine($"**Rule:** {alert.RuleName}");
            sb.AppendLine($"**Severity:** {alert.Severity.ToUpper()}");
            sb.AppendLine($"**Agent:** {hostname} (ID: {alert.AgentId})");
            sb.AppendLine($"**Category:** {alert.Category}");
            sb.AppendLine($"**Title:** {alert.Title}");
            sb.AppendLine($"**Message:** {alert.Message}");
            sb.AppendLine($"**Time:** {alert.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"**Alert ID:** {alert.Id}");
            return sb.ToString();
        }
    }
}
