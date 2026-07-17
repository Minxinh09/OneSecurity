using System.Threading.Tasks;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class MetricRepository : IMetricRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public MetricRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(MetricRecord record)
        {
            await _dbContext.MetricRecords.AddAsync(record);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
