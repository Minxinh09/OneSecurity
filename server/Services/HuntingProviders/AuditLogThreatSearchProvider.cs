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
    public class AuditLogThreatSearchProvider : IThreatSearchProvider
    {
        private readonly LocalAgentDbContext _dbContext;

        public AuditLogThreatSearchProvider(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string SourceType => "AuditLog";

        public async Task<List<ThreatSearchResultItemDto>> SearchAsync(ThreatSearchRequest request)
        {
            // AuditLog doesn't have hostname or status concept, skip if requested
            if (!string.IsNullOrEmpty(request.Hostname) || !string.IsNullOrEmpty(request.Status))
            {
                return new List<ThreatSearchResultItemDto>();
            }

            var q = _dbContext.AuditLogs.AsNoTracking();

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                q = q.Where(a => a.Action.Contains(request.Keyword) || 
                                 (a.Description != null && a.Description.Contains(request.Keyword)));
            }

            if (!string.IsNullOrEmpty(request.IPAddress))
            {
                q = q.Where(a => a.IpAddress != null && a.IpAddress.Contains(request.IPAddress));
            }

            if (!string.IsNullOrEmpty(request.Username))
            {
                q = q.Where(a => a.UserName != null && a.UserName.Contains(request.Username));
            }

            if (!string.IsNullOrEmpty(request.Severity))
            {
                if (Enum.TryParse<AuditSeverity>(request.Severity, true, out var parsedSev))
                {
                    q = q.Where(a => a.Severity == parsedSev);
                }
                else
                {
                    return new List<ThreatSearchResultItemDto>();
                }
            }

            if (request.From.HasValue)
            {
                q = q.Where(a => a.TimestampUtc >= request.From.Value);
            }

            if (request.To.HasValue)
            {
                q = q.Where(a => a.TimestampUtc <= request.To.Value);
            }

            var audits = await q.OrderByDescending(a => a.TimestampUtc)
                                 .Skip((request.Page - 1) * request.PageSize)
                                 .Take(request.PageSize)
                                 .ToListAsync();

            return audits.Select(a => new ThreatSearchResultItemDto
            {
                Type = "AuditLog",
                Id = a.Id.ToString(),
                Title = a.Action,
                Description = a.Description ?? "",
                Severity = a.Severity.ToString(),
                Status = a.Success ? "Success" : "Failed",
                Hostname = "",
                IpAddress = a.IpAddress ?? "",
                Username = a.UserName ?? "",
                Timestamp = a.TimestampUtc
            }).ToList();
        }
    }
}
