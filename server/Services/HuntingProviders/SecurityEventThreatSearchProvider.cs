using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services.HuntingProviders
{
    public class SecurityEventThreatSearchProvider : IThreatSearchProvider
    {
        private readonly LocalAgentDbContext _dbContext;

        public SecurityEventThreatSearchProvider(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string SourceType => "SecurityEvent";

        public async Task<List<ThreatSearchResultItemDto>> SearchAsync(ThreatSearchRequest request)
        {
            // SecurityEvent doesn't have username or status concept, skip if requested
            if (!string.IsNullOrEmpty(request.Username) || !string.IsNullOrEmpty(request.Status))
            {
                return new List<ThreatSearchResultItemDto>();
            }

            var q = _dbContext.SecurityEvents.Include(e => e.Agent).AsNoTracking();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                q = q.Where(e => e.Title.Contains(request.Keyword) || 
                                 e.Details.Contains(request.Keyword) || 
                                 e.Category.Contains(request.Keyword) || 
                                 e.Source.Contains(request.Keyword));
            }

            if (!string.IsNullOrEmpty(request.Hostname))
            {
                q = q.Where(e => e.Agent != null && e.Agent.Hostname.Contains(request.Hostname));
            }

            if (!string.IsNullOrEmpty(request.IPAddress))
            {
                q = q.Where(e => e.Agent != null && e.Agent.IpAddress.Contains(request.IPAddress));
            }

            if (!string.IsNullOrEmpty(request.Severity))
            {
                q = q.Where(e => e.Severity == request.Severity);
            }

            if (request.From.HasValue)
            {
                q = q.Where(e => e.Timestamp >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                q = q.Where(e => e.Timestamp <= request.To.Value);
            }

            var events = await q.OrderByDescending(e => e.Timestamp)
                                 .Skip((request.Page - 1) * request.PageSize)
                                 .Take(request.PageSize)
                                 .ToListAsync();

            return events.Select(e => new ThreatSearchResultItemDto
            {
                Type = "SecurityEvent",
                Id = e.Id.ToString(),
                Title = e.Title,
                Description = e.Details,
                Severity = e.Severity,
                Status = "",
                Hostname = e.Agent?.Hostname ?? "Unknown",
                IpAddress = e.Agent?.IpAddress ?? "N/A",
                Username = "",
                Timestamp = e.Timestamp
            }).ToList();
        }
    }
}
