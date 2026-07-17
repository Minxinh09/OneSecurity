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
    public class IncidentRepository : IIncidentRepository
    {
        private readonly LocalAgentDbContext _context;

        public IncidentRepository(LocalAgentDbContext context)
        {
            _context = context;
        }

        public async Task<IncidentListResponse> GetPagedAsync(
            int page, 
            int pageSize, 
            string? status = null, 
            string? severity = null, 
            string? assignedUserId = null, 
            string? searchQuery = null)
        {
            var query = _context.Incidents
                .AsNoTracking()
                .Include(i => i.AssignedUser)
                .Include(i => i.CreatedBy)
                .Include(i => i.Alerts)
                .AsQueryable();

            // Apply Filters
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<IncidentStatus>(status, true, out var statusEnum))
                {
                    query = query.Where(i => i.Status == statusEnum);
                }
            }

            if (!string.IsNullOrWhiteSpace(severity))
            {
                if (Enum.TryParse<IncidentSeverity>(severity, true, out var severityEnum))
                {
                    query = query.Where(i => i.Severity == severityEnum);
                }
            }

            if (!string.IsNullOrWhiteSpace(assignedUserId))
            {
                query = query.Where(i => i.AssignedUserId == assignedUserId);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var searchLower = searchQuery.ToLower();
                query = query.Where(i => 
                    i.Title.ToLower().Contains(searchLower) || 
                    i.Description.ToLower().Contains(searchLower));
            }

            // Newest first
            query = query.OrderByDescending(i => i.CreatedAt);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new IncidentDto
                {
                    Id = i.Id,
                    Title = i.Title,
                    Description = i.Description,
                    Severity = i.Severity.ToString(),
                    Status = i.Status.ToString(),
                    AssignedUserId = i.AssignedUserId,
                    AssignedUserName = i.AssignedUser != null ? i.AssignedUser.UserName : null,
                    AssignedAt = i.AssignedAt,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt,
                    AlertCount = i.Alerts.Count,
                    CreatedBy = i.CreatedBy != null ? i.CreatedBy.UserName : null
                })
                .ToListAsync();

            return new IncidentListResponse
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                Items = items
            };
        }

        public async Task<Incident?> GetByIdAsync(long id)
        {
            return await _context.Incidents
                .IgnoreQueryFilters()
                .Include(i => i.AssignedUser)
                .Include(i => i.CreatedBy)
                .Include(i => i.Alerts)
                    .ThenInclude(a => a.Agent)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task AddAsync(Incident incident)
        {
            await _context.Incidents.AddAsync(incident);
        }

        public void Update(Incident incident)
        {
            _context.Incidents.Update(incident);
        }

        public void Delete(Incident incident)
        {
            _context.Incidents.Remove(incident);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
