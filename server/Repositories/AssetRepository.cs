using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class AssetRepository : IAssetRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public AssetRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<InfrastructureAsset?> GetByIdAsync(long id)
        {
            return await _dbContext.InfrastructureAssets
                .Include(a => a.Collector)
                .Include(a => a.Policy)
                .Include(a => a.Agents)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<InfrastructureAsset?> GetByHostnameAsync(string hostname)
        {
            return await _dbContext.InfrastructureAssets
                .Include(a => a.Collector)
                .Include(a => a.Policy)
                .Include(a => a.Agents)
                .FirstOrDefaultAsync(a => a.Hostname.ToLower() == hostname.ToLower());
        }

        public async Task<IEnumerable<InfrastructureAsset>> GetAllAsync()
        {
            return await _dbContext.InfrastructureAssets
                .Include(a => a.Collector)
                .Include(a => a.Policy)
                .Include(a => a.Agents)
                .ToListAsync();
        }

        public async Task AddAsync(InfrastructureAsset asset)
        {
            await _dbContext.InfrastructureAssets.AddAsync(asset);
        }

        public void Update(InfrastructureAsset asset)
        {
            _dbContext.Entry(asset).State = EntityState.Modified;
        }

        public void Delete(InfrastructureAsset asset)
        {
            _dbContext.InfrastructureAssets.Remove(asset);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
