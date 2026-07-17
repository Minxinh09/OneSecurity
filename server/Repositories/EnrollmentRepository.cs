using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public class EnrollmentRepository : IEnrollmentRepository
    {
        private readonly LocalAgentDbContext _dbContext;

        public EnrollmentRepository(LocalAgentDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EnrollmentToken?> GetByIdAsync(long id)
        {
            return await _dbContext.EnrollmentTokens
                .Include(t => t.Asset)
                .Include(t => t.Policy)
                .Include(t => t.Collector)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<EnrollmentToken?> GetByTokenAsync(string token)
        {
            return await _dbContext.EnrollmentTokens
                .Include(t => t.Asset)
                .Include(t => t.Policy)
                .Include(t => t.Collector)
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task<IEnumerable<EnrollmentToken>> GetAllAsync()
        {
            return await _dbContext.EnrollmentTokens
                .Include(t => t.Asset)
                .Include(t => t.Policy)
                .Include(t => t.Collector)
                .ToListAsync();
        }

        public async Task AddAsync(EnrollmentToken enrollmentToken)
        {
            await _dbContext.EnrollmentTokens.AddAsync(enrollmentToken);
        }

        public void Update(EnrollmentToken enrollmentToken)
        {
            _dbContext.Entry(enrollmentToken).State = EntityState.Modified;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
