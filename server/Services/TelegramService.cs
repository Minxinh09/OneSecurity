using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface ITelegramService
    {
        Task<bool> SendAlertAsync(Alert alert);
    }

    public class TelegramService : ITelegramService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramService> _logger;
        private readonly HttpClient _httpClient;

        public TelegramService(IServiceProvider serviceProvider, ILogger<TelegramService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<bool> SendAlertAsync(Alert alert)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
                var config = await dbContext.Configs.FindAsync(1);

                if (config == null || string.IsNullOrWhiteSpace(config.TelegramBotToken) || string.IsNullOrWhiteSpace(config.TelegramChatId))
                {
                    _logger.LogWarning("Telegram Bot credentials are not configured. Alert not sent to Telegram.");
                    return false;
                }

                string emoji = alert.Severity.ToUpper() == "CRITICAL" ? "🔴 🚨" : "🟡 ⚠️";
                var sb = new StringBuilder();
                sb.AppendLine($"{emoji} <b>[OneSecurity Alert]</b>");
                sb.AppendLine($"<b>Rule:</b> {alert.RuleName}");
                sb.AppendLine($"<b>Severity:</b> {alert.Severity.ToUpper()}");
                sb.AppendLine($"<b>Server:</b> {alert.Server?.Hostname ?? "Unknown"} ({alert.Server?.IpAddress ?? "N/A"})");
                sb.AppendLine($"<b>Category:</b> {alert.Category}");
                sb.AppendLine($"<b>Title:</b> {alert.Title}");
                sb.AppendLine($"<b>Message:</b> {alert.Message}");
                sb.AppendLine($"<b>Time:</b> {alert.CreatedAt:yyyy-MM-dd HH:mm:ss}");

                var payload = new
                {
                    chat_id = config.TelegramChatId,
                    text = sb.ToString(),
                    parse_mode = "HTML"
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                string url = $"https://api.telegram.org/bot{config.TelegramBotToken}/sendMessage";
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully sent alert {AlertId} to Telegram", alert.Id);
                    return true;
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to send alert to Telegram. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending alert to Telegram");
                return false;
            }
        }
    }
}
