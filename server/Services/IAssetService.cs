using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface IAssetService
    {
        Task<InfrastructureAsset?> GetByIdAsync(long id);
        Task<InfrastructureAsset?> GetByHostnameAsync(string hostname);
        Task<IEnumerable<InfrastructureAsset>> GetAllAsync();
        Task<InfrastructureAsset> CreateAsync(InfrastructureAsset asset);
        Task<InfrastructureAsset?> UpdateAsync(long id, InfrastructureAsset asset);
        Task<bool> DeleteAsync(long id);
        Task<bool> TransitionStatusAsync(long id, string targetStatus);
    }
}
