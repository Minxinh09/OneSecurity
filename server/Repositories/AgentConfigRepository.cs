using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class AgentConfigRepository : IAgentConfigRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public AgentConfigRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AgentConfig?> GetDefaultAsync()
        {
            // Xác định cấu hình mặc định tường minh qua trường IsDefault
            return await _dbContext.AgentConfigs
                .FirstOrDefaultAsync(c => c.IsDefault);
        }

        public async Task<AgentConfig?> GetByIdAsync(long id)
        {
            return await _dbContext.AgentConfigs
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _dbContext.AgentConfigs
                .AnyAsync(c => c.Id == id);
        }
    }
}
