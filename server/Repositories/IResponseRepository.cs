using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Repositories
{
    public interface IResponseRepository
    {
        Task<ResponseAction?> GetByIdAsync(long id);
        Task<ResponseAction?> GetByCorrelationIdAsync(string correlationId);
        Task<List<ResponseAction>> GetByIncidentIdAsync(long incidentId);
        Task<List<ResponseAction>> GetPendingActionsAsync();
        Task<(List<ResponseAction> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? status = null, string? actionType = null, string? agentId = null);
        Task AddAsync(ResponseAction action);
        void Update(ResponseAction action);
        Task SaveChangesAsync();
    }
}
