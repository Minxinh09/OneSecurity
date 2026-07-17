using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneSecurity.Server.Data;
using OneSecurity.Server.Hubs;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface IRulesEngine
    {
        Task ProcessEventsAsync(List<SecurityEvent> events);
    }

    public class RulesEngine : IRulesEngine
    {
        private readonly SecurityDbContext _dbContext;
        private readonly IHubContext<SecurityHub> _hubContext;
        private readonly ITelegramService _telegramService;
        private readonly ILogger<RulesEngine> _logger;

        public RulesEngine(
            SecurityDbContext dbContext,
            IHubContext<SecurityHub> hubContext,
            ITelegramService telegramService,
            ILogger<RulesEngine> logger)
        {
            _dbContext = dbContext;
            _hubContext = hubContext;
            _telegramService = telegramService;
            _logger = logger;
        }

        public async Task ProcessEventsAsync(List<SecurityEvent> events)
        {
            var config = await _dbContext.Configs.FirstOrDefaultAsync() ?? new SystemConfig();

            foreach (var ev in events)
            {
                // Ensure Server is loaded with Hospital metadata
                if (ev.Server == null)
                {
                    ev.Server = await _dbContext.Servers.Include(s => s.Hospital).FirstOrDefaultAsync(s => s.Id == ev.ServerId);
                }
                else if (ev.Server.Hospital == null)
                {
                    ev.Server.Hospital = await _dbContext.Hospitals.FindAsync(ev.Server.HospitalId);
                }

                // Broadcast event in real-time to dashboard securely (SuperAdmins & server's Hospital group)
                var hospitalGroup = $"Hospital_{ev.Server?.HospitalId ?? 0}";
                await _hubContext.Clients.Groups("SuperAdmins", hospitalGroup).SendAsync("ReceiveEvent", ev);

                // Run rules
                await CheckBruteForceRuleAsync(ev, config);
                await CheckAfterHoursLoginRuleAsync(ev);
                await CheckHighPrivilegeRuleAsync(ev);
                await CheckServiceDownRuleAsync(ev);
                await CheckBackupMissedRuleAsync(ev);
                await CheckUserMgmtRuleAsync(ev);
                await CheckFirewallRuleAsync(ev);
                await CheckRootSshRuleAsync(ev);
                await CheckCrontabRuleAsync(ev);
                await CheckSqlInjectionRuleAsync(ev);
                await CheckXssRuleAsync(ev);
                await CheckPathTraversalRuleAsync(ev);
                await CheckWebFloodRuleAsync(ev);
            }
        }

        private async Task TriggerAlertAsync(SecurityEvent sourceEvent, string ruleName, string severity, string title, string message)
        {
            _logger.LogInformation("Triggering Alert for rule {RuleName} on server {ServerId}", ruleName, sourceEvent.ServerId);

            var alert = new Alert
            {
                ServerId = sourceEvent.ServerId,
                Server = sourceEvent.Server,
                RuleName = ruleName,
                Severity = severity,
                Title = title,
                Message = message,
                Category = sourceEvent.Category,
                CreatedAt = DateTime.UtcNow,
                IsAcknowledged = false,
                TelegramSent = false
            };

            _dbContext.Alerts.Add(alert);
            await _dbContext.SaveChangesAsync();

            // Push to dashboard clients via SignalR securely (SuperAdmins & server's Hospital group)
            var hospitalGroup = $"Hospital_{alert.Server?.HospitalId ?? 0}";
            await _hubContext.Clients.Groups("SuperAdmins", hospitalGroup).SendAsync("ReceiveAlert", alert);

            // Send to Telegram
            bool sent = await _telegramService.SendAlertAsync(alert);
            if (sent)
            {
                alert.TelegramSent = true;
                _dbContext.Entry(alert).Property(x => x.TelegramSent).IsModified = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task CheckBruteForceRuleAsync(SecurityEvent ev, SystemConfig config)
        {
            // Windows event 4625 or Unix ssh fail
            bool isFailedLogin = (ev.Category == "login" && 
                                 (ev.Title.Contains("failed", StringComparison.OrdinalIgnoreCase) || 
                                  ev.Details.Contains("fail", StringComparison.OrdinalIgnoreCase)));

            if (!isFailedLogin) return;

            // Extract source IP
            string? ipAddress = ExtractIpAddress(ev.Details) ?? ExtractIpAddress(ev.RawData);
            if (string.IsNullOrEmpty(ipAddress)) ipAddress = "unknown";

            // Count failed logins from same IP in last X minutes
            var cutoffTime = DateTime.UtcNow.AddMinutes(-config.BruteForceWindowMinutes);
            
            var failedCount = await _dbContext.Events
                .Where(e => e.ServerId == ev.ServerId &&
                            e.Category == "login" &&
                            e.Timestamp >= cutoffTime &&
                            (e.Title.ToLower().Contains("failed") || 
                             e.Details.ToLower().Contains("fail")))
                .ToListAsync();

            // Filter by IP (since IP extraction can be complex, match inside Details or RawData)
            int ipCount = failedCount.Count(e => e.Details.Contains(ipAddress) || e.RawData.Contains(ipAddress)) + 1; // +1 for the current event

            if (ipCount >= config.BruteForceThreshold)
            {
                // Verify if we already raised a brute-force alert for this IP in the last window to prevent alert flooding
                var existingAlertCutoff = DateTime.UtcNow.AddMinutes(-config.BruteForceWindowMinutes);
                var alertAlreadyRaised = await _dbContext.Alerts
                    .AnyAsync(a => a.ServerId == ev.ServerId &&
                                   a.RuleName == "Brute-force" &&
                                   a.CreatedAt >= existingAlertCutoff &&
                                   a.Message.Contains(ipAddress));

                if (!alertAlreadyRaised)
                {
                    string serverName = ev.Server?.Hostname ?? "Server";
                    await TriggerAlertAsync(
                        ev,
                        "Brute-force",
                        "CRITICAL",
                        $"Brute-force attack detected on {serverName}",
                        $"Detected {ipCount} failed login attempts from IP {ipAddress} in the last {config.BruteForceWindowMinutes} minutes."
                    );
                }
            }
        }

        private async Task CheckAfterHoursLoginRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "login" || ev.Title.Contains("failed", StringComparison.OrdinalIgnoreCase))
                return; // Only watch successful logins

            // Timezone is assumed to be local. Standard office hours: 06:00 - 22:00
            // Since Timestamp is UTC, we convert to local time of the event (or server local time)
            // For MVP simplicity, we parse local time of the host or use local machine time.
            var localTime = ev.Timestamp.ToLocalTime();
            int hour = localTime.Hour;

            if (hour < 6 || hour >= 22)
            {
                string serverName = ev.Server?.Hostname ?? "Server";
                await TriggerAlertAsync(
                    ev,
                    "After-hours login",
                    "WARNING",
                    $"After-hours login on {serverName}",
                    $"User logged in successfully at {localTime:HH:mm:ss} (outside business hours 06:00-22:00)."
                );
            }
        }

        private async Task CheckHighPrivilegeRuleAsync(SecurityEvent ev)
        {
            bool isHighPrivilege = ev.Category == "privilege" || 
                                   ev.Details.Contains("SA login", StringComparison.OrdinalIgnoreCase) ||
                                   ev.Details.Contains("SYS login", StringComparison.OrdinalIgnoreCase) ||
                                   ev.Details.Contains("SYSDBA", StringComparison.OrdinalIgnoreCase) ||
                                   ev.Details.Contains("sudo su", StringComparison.OrdinalIgnoreCase);

            if (isHighPrivilege)
            {
                string serverName = ev.Server?.Hostname ?? "Server";
                await TriggerAlertAsync(
                    ev,
                    "High-privilege usage",
                    "CRITICAL",
                    $"High-privilege account login on {serverName}",
                    $"Sensitive database or root administration credential usage detected: {ev.Title}. Details: {ev.Details}"
                );
            }
        }

        private async Task CheckServiceDownRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "service") return;

            // Service stop detection
            bool isServiceStopped = ev.Details.Contains("stopped", StringComparison.OrdinalIgnoreCase) || 
                                    ev.Details.Contains("inactive", StringComparison.OrdinalIgnoreCase);

            if (isServiceStopped && (ev.Title.Contains("SQL", StringComparison.OrdinalIgnoreCase) || 
                                     ev.Title.Contains("IIS", StringComparison.OrdinalIgnoreCase) || 
                                     ev.Title.Contains("apache", StringComparison.OrdinalIgnoreCase) ||
                                     ev.Title.Contains("oracle", StringComparison.OrdinalIgnoreCase)))
            {
                string serverName = ev.Server?.Hostname ?? "Server";
                await TriggerAlertAsync(
                    ev,
                    "Service down",
                    "CRITICAL",
                    $"Critical service stopped on {serverName}",
                    $"Critical service '{ev.Title}' has stopped or failed. Status is inactive."
                );
            }
        }

        private async Task CheckBackupMissedRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "backup") return;

            bool isBackupFailed = ev.Title.Contains("failed", StringComparison.OrdinalIgnoreCase) || 
                                  ev.Details.Contains("fail", StringComparison.OrdinalIgnoreCase) || 
                                  ev.Severity == "critical" || 
                                  ev.Severity == "warning";

            if (isBackupFailed)
            {
                string serverName = ev.Server?.Hostname ?? "Server";
                await TriggerAlertAsync(
                    ev,
                    "Backup missed",
                    "CRITICAL",
                    $"Database backup failed on {serverName}",
                    $"System backup operation reported failure: {ev.Title}. Detail: {ev.Details}"
                );
            }
        }

        private async Task CheckUserMgmtRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "user_mgmt") return;

            string serverName = ev.Server?.Hostname ?? "Server";
            await TriggerAlertAsync(
                ev,
                "User management",
                "WARNING",
                $"User management event on {serverName}",
                $"Account creation or group modification detected: {ev.Title}. Detail: {ev.Details}"
            );
        }

        private async Task CheckFirewallRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "firewall") return;

            string serverName = ev.Server?.Hostname ?? "Server";
            await TriggerAlertAsync(
                ev,
                "Firewall change",
                "WARNING",
                $"Firewall rules modified on {serverName}",
                $"Network rules addition, deletion or modification event detected: {ev.Title}"
            );
        }

        private async Task CheckRootSshRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "login") return;

            bool isRootSsh = ev.Source == "sshlog" && 
                             (ev.Details.Contains("root", StringComparison.OrdinalIgnoreCase) || 
                              ev.Title.Contains("root", StringComparison.OrdinalIgnoreCase));

            if (isRootSsh && !ev.Title.Contains("failed", StringComparison.OrdinalIgnoreCase))
            {
                string serverName = ev.Server?.Hostname ?? "Server";
                await TriggerAlertAsync(
                    ev,
                    "Root SSH login",
                    "CRITICAL",
                    $"Direct Root SSH login on {serverName}",
                    $"Direct SSH session established using Root account. Source: {ev.Details}"
                );
            }
        }

        private async Task CheckCrontabRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "crontab") return;

            string serverName = ev.Server?.Hostname ?? "Server";
            await TriggerAlertAsync(
                ev,
                "Crontab change",
                "WARNING",
                $"Crontab scheduled task modified on {serverName}",
                $"Task scheduling configuration (crontab/fsnotify) modified: {ev.Title}"
            );
        }

        private async Task CheckSqlInjectionRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "sql_injection") return;

            string serverName = ev.Server?.Hostname ?? "Server";
            await TriggerAlertAsync(
                ev,
                "Web Intrusion",
                "CRITICAL",
                $"SQL Injection attack on {serverName}",
                $"Potential SQL Injection attack payload detected in HTTP request parameters. Source details: {ev.Details}"
            );
        }

        private async Task CheckXssRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "xss") return;

            string serverName = ev.Server?.Hostname ?? "Server";
            await TriggerAlertAsync(
                ev,
                "Web Intrusion",
                "CRITICAL",
                $"Cross-Site Scripting (XSS) attack on {serverName}",
                $"Potential XSS attack payload detected in HTTP request parameters. Source details: {ev.Details}"
            );
        }

        private async Task CheckPathTraversalRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "path_traversal") return;

            string serverName = ev.Server?.Hostname ?? "Server";
            await TriggerAlertAsync(
                ev,
                "Web Intrusion",
                "CRITICAL",
                $"Directory Traversal attack on {serverName}",
                $"Potential Directory Traversal attack payload detected in HTTP request parameters. Source details: {ev.Details}"
            );
        }

        private async Task CheckWebFloodRuleAsync(SecurityEvent ev)
        {
            if (ev.Category != "web_flood") return;

            string serverName = ev.Server?.Hostname ?? "Server";
            await TriggerAlertAsync(
                ev,
                "Web DoS",
                "WARNING",
                $"Request flood detected on {serverName}",
                $"High request rate detected: {ev.Details}"
            );
        }

        private string? ExtractIpAddress(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var match = Regex.Match(text, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
            return match.Success ? match.Value : null;
        }
    }
}
