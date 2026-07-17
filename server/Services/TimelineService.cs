using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public class TimelineService : ITimelineService
    {
        private readonly LocalAgentDbContext _dbContext;

        public TimelineService(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<TimelineItemDto>> GetUnifiedTimelineAsync(DateTime? from = null)
        {
            var cutoff = from ?? DateTime.UtcNow.AddDays(-7);

            var events = await _dbContext.SecurityEvents
                .AsNoTracking()
                .Where(e => e.Timestamp >= cutoff)
                .OrderByDescending(e => e.Timestamp)
                .Take(200)
                .Select(e => new TimelineItemDto
                {
                    Id = e.Id.ToString(),
                    Type = "SecurityEvent",
                    Title = e.Title,
                    Description = $"Event from host {(e.Agent != null ? e.Agent.Hostname : "Unknown")}: {e.Details}",
                    Timestamp = e.Timestamp,
                    UserName = null,
                    Severity = e.Severity,
                    ReferenceId = e.EventId
                }).ToListAsync();

            var alerts = await _dbContext.Alerts
                .AsNoTracking()
                .Where(a => a.CreatedAt >= cutoff)
                .OrderByDescending(a => a.CreatedAt)
                .Take(200)
                .Select(a => new TimelineItemDto
                {
                    Id = a.Id.ToString(),
                    Type = "Alert",
                    Title = a.Title,
                    Description = $"Alert rule '{a.RuleName}' triggered on {(a.Agent != null ? a.Agent.Hostname : "Unknown")}: {a.Message}",
                    Timestamp = a.CreatedAt,
                    UserName = null,
                    Severity = a.Severity,
                    ReferenceId = a.Id.ToString()
                }).ToListAsync();

            var audits = await _dbContext.AuditLogs
                .AsNoTracking()
                .Where(a => a.TimestampUtc >= cutoff && 
                            (a.Action.StartsWith("Incident") || a.Action.Contains("Alert") || a.Action.Contains("User") || a.Action.Contains("Auth")))
                .OrderByDescending(a => a.TimestampUtc)
                .Take(200)
                .Select(a => new TimelineItemDto
                {
                    Id = a.Id.ToString(),
                    Type = "Audit",
                    Title = a.Action,
                    Description = a.Description ?? string.Empty,
                    Timestamp = a.TimestampUtc,
                    UserName = a.UserName,
                    Severity = a.Severity.ToString(),
                    ReferenceId = a.Id.ToString()
                }).ToListAsync();

            var incidents = await _dbContext.Incidents
                .AsNoTracking()
                .Where(i => i.CreatedAt >= cutoff)
                .OrderByDescending(i => i.CreatedAt)
                .Take(200)
                .Select(i => new TimelineItemDto
                {
                    Id = i.Id.ToString(),
                    Type = "Incident",
                    Title = i.Title,
                    Description = $"Incident severity '{i.Severity}' status '{i.Status}': {i.Description}",
                    Timestamp = i.CreatedAt,
                    UserName = i.AssignedUser != null ? i.AssignedUser.UserName : (i.CreatedBy != null ? i.CreatedBy.UserName : null),
                    Severity = i.Severity.ToString(),
                    ReferenceId = i.Id.ToString()
                }).ToListAsync();

            var unified = events.Concat(alerts).Concat(audits).Concat(incidents)
                .OrderByDescending(x => x.Timestamp)
                .Take(200)
                .ToList();

            return unified;
        }
    }
}
