using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IDashboardRepository
    {
        Task<int> GetTotalAgentsCountAsync();
        Task<int> GetOnlineAgentsCountAsync();
        Task<int> GetOfflineAgentsCountAsync();
        Task<int> GetTotalEventsCountAsync();
        Task<int> GetTotalAlertsCountAsync();
        Task<int> GetUnresolvedAlertsCountAsync();
        Task<List<Alert>> GetRecentAlertsAsync(int count);
        Task<List<SecurityEvent>> GetRecentEventsAsync(int count);
        Task<List<Agent>> GetAllAgentsStatusAsync();
    }
}
