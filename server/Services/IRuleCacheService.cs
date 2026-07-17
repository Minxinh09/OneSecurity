using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface IRuleCacheService
    {
        Task<List<AlertRule>> GetActiveRulesAsync();
        Task ReloadAsync();
    }
}
