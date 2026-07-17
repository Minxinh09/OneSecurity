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
using OneSecurity.Server.Realtime;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
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
                    await CheckOfflineAgentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for offline agents.");
                }

                // Check every 15 seconds
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }

            _logger.LogInformation("Heartbeat Monitor Background Service is stopping.");
        }

        private async Task CheckOfflineAgentsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LocalAgentDbContext>();
            var notificationHubService = scope.ServiceProvider.GetRequiredService<INotificationHubService>();
            var configRepo = scope.ServiceProvider.GetRequiredService<IAgentConfigRepository>();

            // Tắt Global Query Filter để Background Service có quyền quét toàn bộ Server của mọi Bệnh viện
            dbContext.FilterOverride = null;

            var config = await configRepo.GetDefaultAsync();
            int timeoutSeconds = config?.HeartbeatIntervalSeconds * 3 ?? 30; // Chờ 3 nhịp tim bị lỡ thì báo sập
            var cutoffTime = DateTime.UtcNow.AddSeconds(-timeoutSeconds);

            var offlineAgents = await dbContext.Agents
                .Where(a => a.Status != "offline" && a.LastSeenAt < cutoffTime)
                .ToListAsync(cancellationToken);

            foreach (var agent in offlineAgents)
            {
                _logger.LogWarning("Agent {AgentId} ({Hostname}) is offline. Last heartbeat: {LastSeenAt}", 
                    agent.Id, agent.Hostname, agent.LastSeenAt);

                var oldStatus = agent.Status;
                agent.Status = "offline";
                dbContext.Entry(agent).Property(x => x.Status).IsModified = true;

                // Create alert
                var alert = new Alert
                {
                    AgentId = agent.Id,
                    Agent = agent,
                    RuleName = "Agent offline",
                    Severity = "critical",
                    Title = $"Agent offline alert for {agent.Hostname}",
                    Message = $"No heartbeat received from agent '{agent.Id}' for more than {timeoutSeconds} seconds. Server might be down or disconnected.",
                    Category = "service",
                    CreatedAt = DateTime.UtcNow,
                    IsAcknowledged = false,
                    TelegramSent = false
                };

                dbContext.Alerts.Add(alert);
                await dbContext.SaveChangesAsync(cancellationToken);

                // Broadcast thông qua kiến trúc Multi-tenant mới
                await notificationHubService.NotifyAgentStatusChangedAsync(agent, oldStatus, "offline");
                await notificationHubService.NotifyAlertCreatedAsync(alert);
            }
        }
    }
}