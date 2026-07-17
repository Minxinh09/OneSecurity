using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services.HuntingProviders
{
    public class AgentThreatSearchProvider : IThreatSearchProvider
    {
        private readonly LocalAgentDbContext _dbContext;

        public AgentThreatSearchProvider(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string SourceType => "Agent";

        public async Task<List<ThreatSearchResultItemDto>> SearchAsync(ThreatSearchRequest request)
        {
            // Agent doesn't have username or severity concept, skip query if they are specified
            if (!string.IsNullOrEmpty(request.Username) || !string.IsNullOrEmpty(request.Severity))
            {
                return new List<ThreatSearchResultItemDto>();
            }

            var q = _dbContext.Agents.AsNoTracking();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                q = q.Where(a => a.Hostname.Contains(request.Keyword) || 
                                 a.IpAddress.Contains(request.Keyword) || 
                                 (a.OsInfo != null && a.OsInfo.Contains(request.Keyword)));
            }

            if (!string.IsNullOrEmpty(request.Hostname))
            {
                q = q.Where(a => a.Hostname.Contains(request.Hostname));
            }

            if (!string.IsNullOrEmpty(request.IPAddress))
            {
                q = q.Where(a => a.IpAddress.Contains(request.IPAddress));
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                q = q.Where(a => a.Status == request.Status);
            }

            if (request.From.HasValue)
            {
                q = q.Where(a => a.RegisteredAt >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                q = q.Where(a => a.RegisteredAt <= request.To.Value);
            }

            var agents = await q.OrderByDescending(a => a.RegisteredAt)
                                 .Skip((request.Page - 1) * request.PageSize)
                                 .Take(request.PageSize)
                                 .ToListAsync();

            return agents.Select(a => new ThreatSearchResultItemDto
            {
                Type = "Agent",
                Id = a.Id,
                Title = a.Hostname,
                Description = $"Registered Host. OS: {a.OsInfo}, IP: {a.IpAddress}",
                Severity = "Information",
                Status = a.Status,
                Hostname = a.Hostname,
                IpAddress = a.IpAddress,
                Username = "",
                Timestamp = a.RegisteredAt
            }).ToList();
        }
    }
}
