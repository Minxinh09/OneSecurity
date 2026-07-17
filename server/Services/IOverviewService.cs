using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IOverviewService
    {
        Task<DashboardOverviewDto> GetOverviewAsync(string? currentUserId);
        Task<DashboardOverviewUpdatedDto> GetLightweightOverviewAsync(string? currentUserId);
    }
}
