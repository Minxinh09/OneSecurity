using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IResponseActionRepository
    {
        Task<ResponseAction?> GetByIdAsync(long id);
        Task<List<ResponseAction>> GetByAgentIdAsync(string agentId);
        Task<List<ResponseAction>> GetPendingActionsAsync();
        Task<ResponseAction?> GetNextPendingActionAsync(string agentId);
        Task<(List<ResponseAction> Items, int TotalCount)> GetPagedAsync(
            int page, 
            int pageSize, 
            string? status = null, 
            string? actionType = null, 
            string? agentId = null,
            string? requestedBy = null);
        Task AddAsync(ResponseAction action);
        void Update(ResponseAction action);
        Task SaveChangesAsync();
    }
}
