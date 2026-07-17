using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Services.HuntingProviders
{
    public class IncidentThreatSearchProvider : IThreatSearchProvider
    {
        private readonly LocalAgentDbContext _dbContext;

        public IncidentThreatSearchProvider(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string SourceType => "Incident";

        public async Task<List<ThreatSearchResultItemDto>> SearchAsync(ThreatSearchRequest request)
        {
            // Incident doesn't have agent hostname or IP concept directly associated, skip if requested
            if (!string.IsNullOrEmpty(request.Hostname) || !string.IsNullOrEmpty(request.IPAddress))
            {
                return new List<ThreatSearchResultItemDto>();
            }

            var q = _dbContext.Incidents.Include(i => i.AssignedUser).Include(i => i.CreatedBy).AsNoTracking();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                q = q.Where(i => i.Title.Contains(request.Keyword) || i.Description.Contains(request.Keyword));
            }

            if (!string.IsNullOrEmpty(request.Username))
            {
                q = q.Where(i => (i.AssignedUser != null && i.AssignedUser.UserName.Contains(request.Username)) || 
                                 (i.CreatedBy != null && i.CreatedBy.UserName.Contains(request.Username)));
            }

            if (!string.IsNullOrEmpty(request.Severity))
            {
                if (Enum.TryParse<IncidentSeverity>(request.Severity, true, out var parsedSev))
                {
                    q = q.Where(i => i.Severity == parsedSev);
                }
                else
                {
                    return new List<ThreatSearchResultItemDto>();
                }
            }

            if (!string.IsNullOrEmpty(request.Status))
            {
                if (Enum.TryParse<IncidentStatus>(request.Status, true, out var parsedStatus))
                {
                    q = q.Where(i => i.Status == parsedStatus);
                }
                else
                {
                    return new List<ThreatSearchResultItemDto>();
                }
            }

            if (request.From.HasValue)
            {
                q = q.Where(i => i.CreatedAt >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                q = q.Where(i => i.CreatedAt <= request.To.Value);
            }

            var incidents = await q.OrderByDescending(i => i.CreatedAt)
                                   .Skip((request.Page - 1) * request.PageSize)
                                   .Take(request.PageSize)
                                   .ToListAsync();

            return incidents.Select(i => new ThreatSearchResultItemDto
            {
                Type = "Incident",
                Id = i.Id.ToString(),
                Title = i.Title,
                Description = i.Description,
                Severity = i.Severity.ToString(),
                Status = i.Status.ToString(),
                Hostname = "",
                IpAddress = "",
                Username = i.AssignedUser != null ? i.AssignedUser.UserName : (i.CreatedBy != null ? i.CreatedBy.UserName : ""),
                Timestamp = i.CreatedAt
            }).ToList();
        }
    }
}
