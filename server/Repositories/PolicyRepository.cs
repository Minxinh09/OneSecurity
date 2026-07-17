using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public PolicyRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AgentPolicy?> GetByIdAsync(long id)
        {
            return await _dbContext.AgentPolicies.FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<AgentPolicy>> GetAllAsync()
        {
            return await _dbContext.AgentPolicies.ToListAsync();
        }

        public async Task AddAsync(AgentPolicy policy)
        {
            await _dbContext.AgentPolicies.AddAsync(policy);
        }

        public void Update(AgentPolicy policy)
        {
            _dbContext.Entry(policy).State = EntityState.Modified;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
