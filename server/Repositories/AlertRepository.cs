using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class AlertRepository : IAlertRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public AlertRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Alert alert)
        {
            if (alert.Rule != null && _dbContext.Entry(alert.Rule).State == EntityState.Detached)
            {
                _dbContext.Attach(alert.Rule);
            }

            if (alert.Agent != null && _dbContext.Entry(alert.Agent).State == EntityState.Detached)
            {
                _dbContext.Attach(alert.Agent);
            }

            await _dbContext.Alerts.AddAsync(alert);
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _dbContext.Alerts
                .AsNoTracking()
                .AnyAsync(a => a.Id == id);
        }

        public async Task<Alert?> GetByIdAsync(long id)
        {
            return await _dbContext.Alerts
                .AsNoTracking()
                .Include(a => a.Agent)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<Alert>> GetByIdsAsync(IEnumerable<long> ids)
        {
            return await _dbContext.Alerts
                .AsNoTracking()
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();
        }

        public async Task<List<Alert>> GetPagedAsync(
            string? severity,
            string? category,
            string? agentId,
            bool? isAcknowledged,
            int page,
            int pageSize)
        {
            var query = _dbContext.Alerts
                .AsNoTracking()
                .Include(a => a.Agent)
                .AsQueryable();

            query = ApplyFilters(query, severity, category, agentId, isAcknowledged);

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(
            string? severity,
            string? category,
            string? agentId,
            bool? isAcknowledged)
        {
            var query = _dbContext.Alerts
                .AsNoTracking()
                .AsQueryable();

            query = ApplyFilters(query, severity, category, agentId, isAcknowledged);

            return await query.CountAsync();
        }

        public void Update(Alert alert)
        {
            _dbContext.Alerts.Update(alert);
        }

        public void UpdateRange(IEnumerable<Alert> alerts)
        {
            _dbContext.Alerts.UpdateRange(alerts);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }

        private IQueryable<Alert> ApplyFilters(
            IQueryable<Alert> query,
            string? severity,
            string? category,
            string? agentId,
            bool? isAcknowledged)
        {
            if (!string.IsNullOrWhiteSpace(severity))
            {
                query = query.Where(a => a.Severity == severity);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(a => a.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(agentId))
            {
                query = query.Where(a => a.AgentId == agentId);
            }

            if (isAcknowledged.HasValue)
            {
                query = query.Where(a => a.IsAcknowledged == isAcknowledged.Value);
            }

            return query;
        }
    }
}
