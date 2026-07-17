using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class AgentRepository : IAgentRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public AgentRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Agent?> GetByIdAsync(string id)
        {
            // Không tự ý Include theo đúng Quy tắc số 7
            return await _dbContext.Agents
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Agent?> GetByHostnameAsync(string hostname)
        {
            return await _dbContext.Agents
                .FirstOrDefaultAsync(a => a.Hostname == hostname);
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _dbContext.Agents
                .AnyAsync(a => a.Id == id);
        }

        public async Task AddAsync(Agent agent)
        {
            await _dbContext.Agents.AddAsync(agent);
        }

        public void Update(Agent agent)
        {
            _dbContext.Entry(agent).State = EntityState.Modified;
        }

        public async Task UpdateHeartbeatAsync(string id)
        {
            // Cập nhật tín hiệu Heartbeat thô (chỉ status và LastSeenAt) trực tiếp xuống DB
            await _dbContext.Agents
                .Where(a => a.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.LastSeenAt, DateTime.UtcNow)
                    .SetProperty(a => a.Status, "online")
                );
        }

        public async Task UpdateStatusAsync(string id, string status)
        {
            await _dbContext.Agents
                .Where(a => a.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, status)
                    .SetProperty(a => a.LastSeenAt, DateTime.UtcNow)
                );
        }

        public async Task<IEnumerable<Agent>> GetOfflineAgentsAsync(DateTime cutoffTime)
        {
            return await _dbContext.Agents
                .Where(a => a.LastSeenAt < cutoffTime && a.Status == "online")
                .ToListAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
