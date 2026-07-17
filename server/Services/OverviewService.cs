using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Services
{
    public class OverviewService : IOverviewService
    {
        private readonly LocalAgentDbContext _dbContext;
        private readonly IMemoryCache _cache;
        private const string CacheKeyPrefix = "DashboardOverview_";
        private const string LightCacheKeyPrefix = "DashboardOverviewLight_";

        public OverviewService(LocalAgentDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<DashboardOverviewDto> GetOverviewAsync(string? currentUserId)
        {
            string cacheKey = CacheKeyPrefix + (currentUserId ?? "global");

            if (!_cache.TryGetValue(cacheKey, out DashboardOverviewDto? overview))
            {
                overview = await FetchOverviewFromDbAsync(currentUserId);
                
                // Cache for 30 seconds
                _cache.Set(cacheKey, overview, TimeSpan.FromSeconds(30));
            }

            return overview!;
        }

        public async Task<DashboardOverviewUpdatedDto> GetLightweightOverviewAsync(string? currentUserId)
        {
            string cacheKey = LightCacheKeyPrefix + (currentUserId ?? "global");

            if (!_cache.TryGetValue(cacheKey, out DashboardOverviewUpdatedDto? lightOverview))
            {
                lightOverview = await FetchLightOverviewFromDbAsync(currentUserId);
                
                // Cache for 10 seconds (lightweight overview can be fresher)
                _cache.Set(cacheKey, lightOverview, TimeSpan.FromSeconds(10));
            }

            return lightOverview!;
        }

        private async Task<DashboardOverviewDto> FetchOverviewFromDbAsync(string? currentUserId)
        {
            var today = DateTime.UtcNow.Date;

            var openIncidents = await _dbContext.Incidents
                .AsNoTracking()
                .CountAsync(i => i.Status != IncidentStatus.Closed && 
                                 i.Status != IncidentStatus.Resolved && 
                                 i.Status != IncidentStatus.FalsePositive);

            var criticalIncidents = await _dbContext.Incidents
                .AsNoTracking()
                .CountAsync(i => i.Severity == IncidentSeverity.Critical && 
                                 i.Status != IncidentStatus.Closed);

            var onlineAgents = await _dbContext.Agents
                .AsNoTracking()
                .CountAsync(a => a.Status == "online");

            var offlineAgents = await _dbContext.Agents
                .AsNoTracking()
                .CountAsync(a => a.Status == "offline");

            var alertsToday = await _dbContext.Alerts
                .AsNoTracking()
                .CountAsync(a => a.CreatedAt >= today);

            var resolvedToday = await _dbContext.Incidents
                .AsNoTracking()
                .CountAsync(i => (i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.FalsePositive) && 
                                 i.ResolvedAt >= today);

            var assignedToMe = 0;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                assignedToMe = await _dbContext.Incidents
                    .AsNoTracking()
                    .CountAsync(i => i.AssignedUserId == currentUserId && 
                                     i.Status != IncidentStatus.Closed);
            }

            // Trend - Last 7 Days
            var sevenDaysAgo = today.AddDays(-6);
            
            var recentAlerts = await _dbContext.Alerts
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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
                .AsNoTracking()
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

        private async Task<DashboardOverviewUpdatedDto> FetchLightOverviewFromDbAsync(string? currentUserId)
        {
            var today = DateTime.UtcNow.Date;

            var openIncidents = await _dbContext.Incidents
                .AsNoTracking()
                .CountAsync(i => i.Status != IncidentStatus.Closed && 
                                 i.Status != IncidentStatus.Resolved && 
                                 i.Status != IncidentStatus.FalsePositive);

            var criticalIncidents = await _dbContext.Incidents
                .AsNoTracking()
                .CountAsync(i => i.Severity == IncidentSeverity.Critical && 
                                 i.Status != IncidentStatus.Closed);

            var onlineAgents = await _dbContext.Agents
                .AsNoTracking()
                .CountAsync(a => a.Status == "online");

            var offlineAgents = await _dbContext.Agents
                .AsNoTracking()
                .CountAsync(a => a.Status == "offline");

            var alertsToday = await _dbContext.Alerts
                .AsNoTracking()
                .CountAsync(a => a.CreatedAt >= today);

            var resolvedToday = await _dbContext.Incidents
                .AsNoTracking()
                .CountAsync(i => (i.Status == IncidentStatus.Resolved || i.Status == IncidentStatus.FalsePositive) && 
                                 i.ResolvedAt >= today);

            var assignedToMe = 0;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                assignedToMe = await _dbContext.Incidents
                    .AsNoTracking()
                    .CountAsync(i => i.AssignedUserId == currentUserId && 
                                     i.Status != IncidentStatus.Closed);
            }

            return new DashboardOverviewUpdatedDto
            {
                OnlineAgents = onlineAgents,
                OfflineAgents = offlineAgents,
                TotalAgents = onlineAgents + offlineAgents,
                OpenIncidents = openIncidents,
                CriticalIncidents = criticalIncidents,
                AlertsToday = alertsToday,
                ResolvedToday = resolvedToday,
                AssignedToMe = assignedToMe
            };
        }
    }
}
