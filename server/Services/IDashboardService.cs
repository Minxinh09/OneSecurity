using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummaryResponse> GetSummaryAsync();
        Task<List<RecentAlertDto>> GetRecentAlertsAsync();
        Task<List<RecentEventDto>> GetRecentEventsAsync();
        Task<List<AgentStatusDto>> GetAgentStatusListAsync();
        Task<DashboardOverviewDto> GetOverviewAsync(string? currentUserId);
        Task<List<TimelineItemDto>> GetUnifiedTimelineAsync();
    }
}
