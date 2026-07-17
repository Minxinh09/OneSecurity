using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly LocalAgentDbContext _dbContext;

        public DashboardService(IDashboardRepository dashboardRepository, LocalAgentDbContext dbContext)
        {
            _dashboardRepository = dashboardRepository;
            _dbContext = dbContext;
        }

        public async Task<DashboardSummaryResponse> GetSummaryAsync()
        {
            var totalAgents = await _dashboardRepository.GetTotalAgentsCountAsync();
            var onlineAgents = await _dashboardRepository.GetOnlineAgentsCountAsync();
            var offlineAgents = await _dashboardRepository.GetOfflineAgentsCountAsync();
            var totalEvents = await _dashboardRepository.GetTotalEventsCountAsync();
            var totalAlerts = await _dashboardRepository.GetTotalAlertsCountAsync();
            var unresolvedAlerts = await _dashboardRepository.GetUnresolvedAlertsCountAsync();

            return new DashboardSummaryResponse
            {
                TotalAgents = totalAgents,
                OnlineAgents = onlineAgents,
                OfflineAgents = offlineAgents,
                TotalEvents = totalEvents,
                TotalAlerts = totalAlerts,
                UnresolvedAlerts = unresolvedAlerts
            };
        }

        public async Task<List<RecentAlertDto>> GetRecentAlertsAsync()
        {
            var alerts = await _dashboardRepository.GetRecentAlertsAsync(5);
            return alerts.Select(a => new RecentAlertDto
            {
                Id = a.Id,
                AgentId = a.AgentId,
                AgentHostname = a.Agent?.Hostname ?? "Unknown Agent",
                RuleName = a.RuleName,
                Severity = a.Severity,
                Title = a.Title,
                Message = a.Message,
                Category = a.Category,
                CreatedAt = a.CreatedAt,
                IsAcknowledged = a.IsAcknowledged
            }).ToList();
        }

        public async Task<List<RecentEventDto>> GetRecentEventsAsync()
        {
            var events = await _dashboardRepository.GetRecentEventsAsync(5);
            return events.Select(e => new RecentEventDto
            {
                Id = e.Id,
                EventId = e.EventId,
                AgentId = e.AgentId,
                AgentHostname = e.Agent?.Hostname ?? "Unknown Agent",
                Timestamp = e.Timestamp,
                Category = e.Category,
                Severity = e.Severity,
                Source = e.Source,
                Title = e.Title,
                Details = e.Details,
                ReceivedAt = e.ReceivedAt
            }).ToList();
        }

        public async Task<List<AgentStatusDto>> GetAgentStatusListAsync()
        {
            var agents = await _dashboardRepository.GetAllAgentsStatusAsync();
            return agents.Select(a => new AgentStatusDto
            {
                AgentId = a.Id,
                Hostname = a.Hostname,
                IpAddress = a.IpAddress,
                Status = a.Status,
                LastSeenAt = a.LastSeenAt
            }).ToList();
        }

        public async Task<DashboardOverviewDto> GetOverviewAsync(string? currentUserId)
        {
            var today = DateTime.UtcNow.Date;

            var openIncidents = await _dbContext.Incidents
                .CountAsync(i => i.Status != IncidentStatus.Closed && i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.FalsePositive);

            var criticalIncidents = await _dbContext.Incidents
                .CountAsync(i => i.Severity == IncidentSeverity.Critical && i.Status != IncidentStatus.Closed);

            var onlineAgents = await _dbContext.Agents.CountAsync(a => a.Status == "online");
            var offlineAgents = await _dbContext.Agents.CountAsync(a => a.Status == "offline");

            var alertsToday = await _dbContext.Alerts.CountAsync(a => a.CreatedAt >= today);

            var resolvedToday = await _dbContext.Incidents
                .CountAsync(i => (i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.FalsePositive) && i.ResolvedAt >= today);

            var assignedToMe = 0;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                assignedToMe = await _dbContext.Incidents
                    .CountAsync(i => i.AssignedUserId == currentUserId && i.Status != IncidentStatus.Closed);
            }

            // Trend - Last 7 Days
            var sevenDaysAgo = today.AddDays(-6);
            
            var recentAlerts = await _dbContext.Alerts
                .Where(a => a.CreatedAt >= sevenDaysAgo)
                .Select(a => a.CreatedAt)
                .ToListAsync();

            var alertTrend = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i))
                .Select(d => new DashboardTrendItem
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    Count = recentAlerts.Count(a => a.Date == d)
                }).ToList();

            var recentIncidents = await _dbContext.Incidents
                .Where(i => i.CreatedAt >= sevenDaysAgo)
                .Select(i => i.CreatedAt)
                .ToListAsync();

            var incidentTrend = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i))
                .Select(d => new DashboardTrendItem
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    Count = recentIncidents.Count(i => i.Date == d)
                }).ToList();

            // Severity Distribution
            var severityList = await _dbContext.Alerts
                .GroupBy(a => a.Severity)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToListAsync();

            var severityDistribution = severityList
                .Select(x => new DashboardKeyValueItem
                {
                    Key = x.Key,
                    Value = x.Count
                }).ToList();

            // Top Rules
            var rulesList = await _dbContext.Alerts
                .GroupBy(a => a.RuleName)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var topAlertRules = rulesList
                .Select(x => new DashboardKeyValueItem
                {
                    Key = x.Key,
                    Value = x.Count
                }).ToList();

            // Top Hosts
            var hostsList = await _dbContext.Alerts
                .GroupBy(a => a.Agent != null ? a.Agent.Hostname : "Unknown Host")
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var topAffectedHosts = hostsList
                .Select(x => new DashboardKeyValueItem
                {
                    Key = x.Key ?? "Unknown Host",
                    Value = x.Count
                }).ToList();

            // Recent Activities (from Audit Logs)
            var recentActivities = await _dbContext.AuditLogs
                .OrderByDescending(a => a.TimestampUtc)
                .Take(10)
                .Select(a => new RecentActivityItemDto
                {
                    Timestamp = a.TimestampUtc,
                    Title = a.Action,
                    Message = a.Description ?? string.Empty,
                    Severity = a.Severity.ToString()
                }).ToListAsync();

            return new DashboardOverviewDto
            {
                OpenIncidents = openIncidents,
                CriticalIncidents = criticalIncidents,
                OnlineAgents = onlineAgents,
                OfflineAgents = offlineAgents,
                AlertsToday = alertsToday,
                ResolvedToday = resolvedToday,
                AssignedToMe = assignedToMe,
                AlertTrend = alertTrend,
                IncidentTrend = incidentTrend,
                AlertSeverityDistribution = severityDistribution,
                TopAlertRules = topAlertRules,
                TopAffectedHosts = topAffectedHosts,
                RecentActivities = recentActivities
            };
        }

        public async Task<List<TimelineItemDto>> GetUnifiedTimelineAsync()
        {
            var events = await _dbContext.SecurityEvents
                .OrderByDescending(e => e.Timestamp)
                .Take(30)
                .Select(e => new TimelineItemDto
                {
                    Id = e.Id.ToString(),
                    Type = "SecurityEvent",
                    Title = e.Title,
                    Description = $"Event from host {(e.Agent != null ? e.Agent.Hostname : "Unknown")}: {e.Details}",
                    Timestamp = e.Timestamp,
                    UserName = null,
                    Severity = e.Severity
                }).ToListAsync();

            var alerts = await _dbContext.Alerts
                .OrderByDescending(a => a.CreatedAt)
                .Take(30)
                .Select(a => new TimelineItemDto
                {
                    Id = a.Id.ToString(),
                    Type = "Alert",
                    Title = a.Title,
                    Description = $"Alert rule '{a.RuleName}' triggered on {(a.Agent != null ? a.Agent.Hostname : "Unknown")}: {a.Message}",
                    Timestamp = a.CreatedAt,
                    UserName = null,
                    Severity = a.Severity
                }).ToListAsync();

            var audits = await _dbContext.AuditLogs
                .Where(a => a.Action.StartsWith("Incident") || a.Action.Contains("Alert") || a.Action.Contains("User") || a.Action.Contains("Auth"))
                .OrderByDescending(a => a.TimestampUtc)
                .Take(30)
                .Select(a => new TimelineItemDto
                {
                    Id = a.Id.ToString(),
                    Type = "Audit",
                    Title = a.Action,
                    Description = a.Description ?? string.Empty,
                    Timestamp = a.TimestampUtc,
                    UserName = a.UserName,
                    Severity = a.Severity.ToString()
                }).ToListAsync();

            var unified = events.Concat(alerts).Concat(audits)
                .OrderByDescending(x => x.Timestamp)
                .Take(50)
                .ToList();

            return unified;
        }
    }
}
