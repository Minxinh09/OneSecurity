using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly LocalAgentDbContext _context;

        public AuditRepository(LocalAgentDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AuditLog auditLog)
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public async Task<AuditLogListResponse> GetPagedAsync(AuditLogFilterRequest filter)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            // Apply Filters
            if (filter.From.HasValue)
            {
                query = query.Where(a => a.TimestampUtc >= filter.From.Value);
            }

            if (filter.To.HasValue)
            {
                query = query.Where(a => a.TimestampUtc <= filter.To.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.User))
            {
                var userLower = filter.User.ToLower();
                query = query.Where(a => a.UserName != null && a.UserName.ToLower().Contains(userLower));
            }

            if (!string.IsNullOrWhiteSpace(filter.Severity))
            {
                if (Enum.TryParse<AuditSeverity>(filter.Severity, true, out var severityEnum))
                {
                    query = query.Where(a => a.Severity == severityEnum);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.Action))
            {
                var actionLower = filter.Action.ToLower();
                query = query.Where(a => a.Action.ToLower().Contains(actionLower));
            }

            if (!string.IsNullOrWhiteSpace(filter.ResourceType))
            {
                if (Enum.TryParse<AuditResourceType>(filter.ResourceType, true, out var resourceTypeEnum))
                {
                    query = query.Where(a => a.ResourceType == resourceTypeEnum);
                }
            }

            if (filter.Success.HasValue)
            {
                query = query.Where(a => a.Success == filter.Success.Value);
            }

            if (filter.StatusCode.HasValue)
            {
                query = query.Where(a => a.StatusCode == filter.StatusCode.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.EntityId))
            {
                query = query.Where(a => a.EntityId == filter.EntityId);
            }

            // Apply Sorting
            var sortDir = filter.SortDirection.ToLower() == "asc" ? "asc" : "desc";
            var sortBy = filter.SortBy.ToLower();

            if (sortDir == "asc")
            {
                query = sortBy switch
                {
                    "severity" => query.OrderBy(a => a.Severity),
                    "action" => query.OrderBy(a => a.Action),
                    "resourcetype" => query.OrderBy(a => a.ResourceType),
                    "username" => query.OrderBy(a => a.UserName),
                    "success" => query.OrderBy(a => a.Success),
                    "statuscode" => query.OrderBy(a => a.StatusCode),
                    _ => query.OrderBy(a => a.TimestampUtc)
                };
            }
            else
            {
                query = sortBy switch
                {
                    "severity" => query.OrderByDescending(a => a.Severity),
                    "action" => query.OrderByDescending(a => a.Action),
                    "resourcetype" => query.OrderByDescending(a => a.ResourceType),
                    "username" => query.OrderByDescending(a => a.UserName),
                    "success" => query.OrderByDescending(a => a.Success),
                    "statuscode" => query.OrderByDescending(a => a.StatusCode),
                    _ => query.OrderByDescending(a => a.TimestampUtc)
                };
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize);

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    TimestampUtc = a.TimestampUtc,
                    UserName = a.UserName,
                    Roles = a.Roles,
                    Action = a.Action,
                    Description = a.Description,
                    ResourceType = a.ResourceType.ToString(),
                    Success = a.Success,
                    StatusCode = a.StatusCode,
                    Severity = a.Severity.ToString()
                })
                .ToListAsync();

            return new AuditLogListResponse
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = filter.Page,
                Items = items
            };
        }

        public async Task<AuditLogDetailDto?> GetByIdAsync(int id)
        {
            var a = await _context.AuditLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (a == null) return null;

            return new AuditLogDetailDto
            {
                Id = a.Id,
                TimestampUtc = a.TimestampUtc,
                UserName = a.UserName,
                Roles = a.Roles,
                Action = a.Action,
                ResourceType = a.ResourceType.ToString(),
                Success = a.Success,
                StatusCode = a.StatusCode,
                Severity = a.Severity.ToString(),
                Description = a.Description,
                IpAddress = a.IpAddress,
                UserAgent = a.UserAgent,
                EntityId = a.EntityId,
                CorrelationId = a.CorrelationId
            };
        }

        public async Task<int> CountAsync()
        {
            return await _context.AuditLogs.CountAsync();
        }
    }
}
