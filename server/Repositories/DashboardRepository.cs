using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public DashboardRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<int> GetTotalAgentsCountAsync()
        {
            return await _dbContext.Agents
                .AsNoTracking()
                .CountAsync();
        }

        public async Task<int> GetOnlineAgentsCountAsync()
        {
            return await _dbContext.Agents
                .AsNoTracking()
                .CountAsync(a => a.Status == "online");
        }

        public async Task<int> GetOfflineAgentsCountAsync()
        {
            return await _dbContext.Agents
                .AsNoTracking()
                .CountAsync(a => a.Status == "offline");
        }

        public async Task<int> GetTotalEventsCountAsync()
        {
            return await _dbContext.SecurityEvents
                .AsNoTracking()
                .CountAsync();
        }

        public async Task<int> GetTotalAlertsCountAsync()
        {
            return await _dbContext.Alerts
                .AsNoTracking()
                .CountAsync();
        }

        public async Task<int> GetUnresolvedAlertsCountAsync()
        {
            return await _dbContext.Alerts
                .AsNoTracking()
                .CountAsync(a => !a.IsAcknowledged);
        }

        public async Task<List<Alert>> GetRecentAlertsAsync(int count)
        {
            return await _dbContext.Alerts
                .AsNoTracking()
                .Include(a => a.Agent)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<SecurityEvent>> GetRecentEventsAsync(int count)
        {
            return await _dbContext.SecurityEvents
                .AsNoTracking()
                .Include(e => e.Agent)
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<Agent>> GetAllAgentsStatusAsync()
        {
            return await _dbContext.Agents
                .AsNoTracking()
                .OrderByDescending(a => a.LastSeenAt)
                .ToListAsync();
        }
    }
}
