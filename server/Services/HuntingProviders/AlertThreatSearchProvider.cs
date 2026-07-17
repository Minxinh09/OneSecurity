using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services.HuntingProviders
{
    public class AlertThreatSearchProvider : IThreatSearchProvider
    {
        private readonly LocalAgentDbContext _dbContext;

        public AlertThreatSearchProvider(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string SourceType => "Alert";

        public async Task<List<ThreatSearchResultItemDto>> SearchAsync(ThreatSearchRequest request)
        {
            // Alert doesn't have username concept, and status is usually different (but let's allow it or skip if not matching)
            if (!string.IsNullOrEmpty(request.Username))
            {
                return new List<ThreatSearchResultItemDto>();
            }

            var q = _dbContext.Alerts.Include(a => a.Agent).AsNoTracking();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                q = q.Where(a => a.Title.Contains(request.Keyword) || 
                                 a.Message.Contains(request.Keyword) || 
                                 a.RuleName.Contains(request.Keyword) || 
                                 a.Category.Contains(request.Keyword));
            }

            if (!string.IsNullOrEmpty(request.Hostname))
            {
                q = q.Where(a => a.Agent != null && a.Agent.Hostname.Contains(request.Hostname));
            }

            if (!string.IsNullOrEmpty(request.IPAddress))
            {
                q = q.Where(a => a.Agent != null && a.Agent.IpAddress.Contains(request.IPAddress));
            }

            if (!string.IsNullOrEmpty(request.Severity))
            {
                q = q.Where(a => a.Severity == request.Severity);
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                // Map status "Acknowledged" or "New" matching alert IsAcknowledged state
                if (request.Status.Equals("Acknowledged", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(a => a.IsAcknowledged);
                }
                else if (request.Status.Equals("New", StringComparison.OrdinalIgnoreCase))
                {
                    q = q.Where(a => !a.IsAcknowledged);
                }
                else
                {
                    // If other status, it doesn't apply to Alerts
                    return new List<ThreatSearchResultItemDto>();
                }
            }

            if (request.From.HasValue)
            {
                q = q.Where(a => a.CreatedAt >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                q = q.Where(a => a.CreatedAt <= request.To.Value);
            }

            var alerts = await q.OrderByDescending(a => a.CreatedAt)
                                 .Skip((request.Page - 1) * request.PageSize)
                                 .Take(request.PageSize)
                                 .ToListAsync();

            return alerts.Select(a => new ThreatSearchResultItemDto
            {
                Type = "Alert",
                Id = a.Id.ToString(),
                Title = a.Title,
                Description = a.Message,
                Severity = a.Severity,
                Status = a.IsAcknowledged ? "Acknowledged" : "New",
                Hostname = a.Agent?.Hostname ?? "Unknown",
                IpAddress = a.Agent?.IpAddress ?? "N/A",
                Username = "",
                Timestamp = a.CreatedAt
            }).ToList();
        }
    }
}
