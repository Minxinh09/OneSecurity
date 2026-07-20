using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IResponseActionService
    {
        Task<ResponseActionDto> CreateAsync(CreateResponseActionRequest request, ClaimsPrincipal user);
        Task<CreateResponseActionResponse> CreateActionAsync(CreateResponseActionRequest request, string userId);
        Task<ResponseActionDto?> GetByIdAsync(long id, string userId);
        Task<List<ResponseActionDto>> GetByAgentIdAsync(string agentId, string userId);
        Task<List<ResponseActionDto>> GetPendingActionsAsync(string userId);
        Task<ResponseActionDto?> CancelActionAsync(long id, string userId);
        Task<ResponseActionListResponse> GetPagedAsync(
            int page, 
            int pageSize, 
            string? status, 
            string? actionType, 
            string? agentId, 
            string? requestedBy,
            string userId);
    }
}
