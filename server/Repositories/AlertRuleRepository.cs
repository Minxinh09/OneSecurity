using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class AlertRuleRepository : IAlertRuleRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public AlertRuleRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<AlertRule>> GetActiveRulesAsync()
        {
            return await _dbContext.AlertRules
                .AsNoTracking()
                .Where(r => r.IsEnabled)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _dbContext.AlertRules
                .AsNoTracking()
                .AnyAsync(r => r.Id == id);
        }

        public async Task<bool> ExistsNameAsync(string name, long? excludeId)
        {
            var query = _dbContext.AlertRules.AsNoTracking();
            if (excludeId.HasValue)
            {
                query = query.Where(r => r.Id != excludeId.Value);
            }
            return await query.AnyAsync(r => r.Name == name);
        }

        public async Task<AlertRule?> GetByIdAsync(long id)
        {
            return await _dbContext.AlertRules
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<AlertRule>> GetPagedAsync(string? name, bool? isEnabled, int page, int pageSize)
        {
            var query = _dbContext.AlertRules
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(r => r.Name.Contains(name));
            }

            if (isEnabled.HasValue)
            {
                query = query.Where(r => r.IsEnabled == isEnabled.Value);
            }

            return await query
                .OrderByDescending(r => r.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountAsync(string? name, bool? isEnabled)
        {
            var query = _dbContext.AlertRules
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(r => r.Name.Contains(name));
            }

            if (isEnabled.HasValue)
            {
                query = query.Where(r => r.IsEnabled == isEnabled.Value);
            }

            return await query.CountAsync();
        }

        public async Task AddAsync(AlertRule rule)
        {
            await _dbContext.AlertRules.AddAsync(rule);
        }

        public void Update(AlertRule rule)
        {
            _dbContext.AlertRules.Update(rule);
        }

        public void Delete(AlertRule rule)
        {
            _dbContext.AlertRules.Remove(rule);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
