using System.Threading.Tasks;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class SecurityEventRepository : ISecurityEventRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public SecurityEventRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(SecurityEvent securityEvent)
        {
            await _dbContext.SecurityEvents.AddAsync(securityEvent);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
