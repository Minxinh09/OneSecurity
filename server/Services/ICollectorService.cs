using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface ICollectorService
    {
        Task<CollectorNode?> GetByIdAsync(long id);
        Task<CollectorNode?> GetByKeyAsync(string key);
        Task<IEnumerable<CollectorNode>> GetAllAsync();
        Task<CollectorNode> CreateAsync(CollectorNode collector);
        Task<CollectorNode?> UpdateAsync(long id, CollectorNode collector);
        Task<CollectorSyncData?> SyncCollectorAsync(long id, string secret, int configVersion, int rulesVersion);
    }
}
