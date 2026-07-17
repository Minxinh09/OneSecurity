using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface ICollectorRepository
    {
        Task<CollectorNode?> GetByIdAsync(long id);
        Task<CollectorNode?> GetByKeyAsync(string key);
        Task<IEnumerable<CollectorNode>> GetAllAsync();
        Task AddAsync(CollectorNode collector);
        void Update(CollectorNode collector);
        Task<int> SaveChangesAsync();
    }
}
