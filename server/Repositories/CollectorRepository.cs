using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class CollectorRepository : ICollectorRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public CollectorRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<CollectorNode?> GetByIdAsync(long id)
        {
            return await _dbContext.CollectorNodes.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<CollectorNode?> GetByKeyAsync(string key)
        {
            return await _dbContext.CollectorNodes.FirstOrDefaultAsync(c => c.CollectorKey == key);
        }

        public async Task<IEnumerable<CollectorNode>> GetAllAsync()
        {
            return await _dbContext.CollectorNodes.ToListAsync();
        }

        public async Task AddAsync(CollectorNode collector)
        {
            await _dbContext.CollectorNodes.AddAsync(collector);
        }

        public void Update(CollectorNode collector)
        {
            _dbContext.Entry(collector).State = EntityState.Modified;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
