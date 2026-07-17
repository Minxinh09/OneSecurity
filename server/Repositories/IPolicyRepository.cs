using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IPolicyRepository
    {
        Task<AgentPolicy?> GetByIdAsync(long id);
        Task<IEnumerable<AgentPolicy>> GetAllAsync();
        Task AddAsync(AgentPolicy policy);
        void Update(AgentPolicy policy);
        Task<int> SaveChangesAsync();
    }
}
