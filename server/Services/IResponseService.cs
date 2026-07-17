using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IResponseService
    {
        Task<ResponseActionDto> RequestActionAsync(RequestResponseActionRequest request, string userId);
        Task<ResponseActionDto?> ApproveActionAsync(long actionId, string adminUserId);
        Task<ResponseActionDto?> CancelActionAsync(long actionId, string adminUserId);
        Task<ResponseActionDto?> GetByIdAsync(long actionId);
        Task<List<ResponseActionDto>> GetByIncidentIdAsync(long incidentId);
        Task<ResponseActionListResponse> GetPagedAsync(int page, int pageSize, string? status = null, string? actionType = null, string? agentId = null);
        Task<bool> UpdateExecutionStatusAsync(string correlationId, string status, string? message);
    }
}
