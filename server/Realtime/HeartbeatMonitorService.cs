using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Realtime
{
    public class HeartbeatMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HeartbeatMonitorService> _logger;

        public HeartbeatMonitorService(
            IServiceProvider serviceProvider,
            ILogger<HeartbeatMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Heartbeat Monitor Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckOfflineAgentsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for offline agents.");
                }

                // Check every 10 seconds
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }

            _logger.LogInformation("Heartbeat Monitor Background Service is stopping.");
        }

        private async Task CheckOfflineAgentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LocalAgentDbContext>();
            var notificationHubService = scope.ServiceProvider.GetRequiredService<INotificationHubService>();
            var telegramNotificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            // Query active agents with their configs
            var activeAgents = await dbContext.Agents
                .Include(a => a.Config)
                .Include(a => a.Asset)
                .Where(a => a.Status != "offline")
                .ToListAsync();

            foreach (var agent in activeAgents)
            {
                // Bỏ qua giám sát offline nếu Asset tương ứng đang bảo trì (Maintenance)
                if (agent.Asset != null && agent.Asset.Status == "Maintenance")
                {
                    continue;
                }

                var interval = agent.Config?.HeartbeatIntervalSeconds ?? 10;
                var timeout = interval * 3; // Timeout is 3 times the interval
                var cutoffTime = DateTime.UtcNow.AddSeconds(-timeout);

                if (agent.LastSeenAt < cutoffTime)
                {
                    _logger.LogWarning("Agent {AgentId} ({Hostname}) is offline. Last heartbeat: {LastSeenAt}", 
                        agent.Id, agent.Hostname, agent.LastSeenAt);

                    var oldStatus = agent.Status;
                    agent.Status = "offline";
                    dbContext.Agents.Update(agent);

                    // Create offline alert
                    var alert = new Alert
                    {
                        AgentId = agent.Id,
                        Agent = agent,
                        RuleName = "Agent offline",
                        Severity = "critical",
                        Title = $"Agent offline alert for {agent.Hostname}",
                        Message = $"No heartbeat received from agent '{agent.Id}' for more than {timeout} seconds. The system may be down or disconnected.",
                        Category = "service",
                        CreatedAt = DateTime.UtcNow,
                        IsAcknowledged = false,
                        TelegramSent = false
                    };

                    dbContext.Alerts.Add(alert);
                    await dbContext.SaveChangesAsync();

                    // Broadcast Realtime Notifications
                    await notificationHubService.NotifyAgentStatusChangedAsync(agent, oldStatus, "offline");
                    await notificationHubService.NotifyAlertCreatedAsync(alert);

                    // Send Telegram alert asynchronously
                    await telegramNotificationService.SendAlertAsync(alert);
                }
            }
        }
    }
}
