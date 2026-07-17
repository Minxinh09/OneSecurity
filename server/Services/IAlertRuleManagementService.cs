using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IAlertRuleManagementService
    {
        Task<AlertRuleListResponse> GetPagedAsync(AlertRuleFilterRequest filter);
        Task<AlertRuleDetailDto?> GetDetailAsync(long id);
        Task<AlertRuleDetailDto?> CreateAsync(CreateAlertRuleRequest request);
        Task<AlertRuleDetailDto?> UpdateAsync(long id, UpdateAlertRuleRequest request);
        Task<bool> EnableAsync(long id);
        Task<bool> DisableAsync(long id);
        Task<bool> DeleteAsync(long id);
    }
}
