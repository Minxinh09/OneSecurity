using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface IPolicyService
    {
        Task<AgentPolicy?> GetByIdAsync(long id);
        Task<IEnumerable<AgentPolicy>> GetAllAsync();
        Task<AgentPolicy> CreateAsync(AgentPolicy policy);
        Task<AgentPolicy?> UpdateAsync(long id, AgentPolicy policy);
    }
}
