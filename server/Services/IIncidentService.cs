using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface IIncidentService
    {
        Task<IncidentDetailDto> CreateAsync(CreateIncidentRequest request, string createdByUserId);
        Task<IncidentDetailDto> AssignAsync(long id, AssignIncidentRequest request, string currentUserId);
        Task<IncidentDetailDto> UpdateStatusAsync(long id, UpdateIncidentStatusRequest request, string currentUserId);
        Task<IncidentDetailDto> LinkAlertsAsync(long id, LinkAlertsRequest request, string currentUserId);
        Task<IncidentDetailDto> UnlinkAlertAsync(long id, long alertId, string currentUserId);
        Task CorrelateAlertAsync(Alert alert);
    }
}
