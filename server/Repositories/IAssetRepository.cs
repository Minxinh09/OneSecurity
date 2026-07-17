using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IAssetRepository
    {
        Task<InfrastructureAsset?> GetByIdAsync(long id);
        Task<InfrastructureAsset?> GetByHostnameAsync(string hostname);
        Task<IEnumerable<InfrastructureAsset>> GetAllAsync();
        Task AddAsync(InfrastructureAsset asset);
        void Update(InfrastructureAsset asset);
        void Delete(InfrastructureAsset asset);
        Task<int> SaveChangesAsync();
    }
}
