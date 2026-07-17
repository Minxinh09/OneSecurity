using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Realtime
{
    public interface INotificationHubService
    {
        Task NotifyAlertCreatedAsync(Alert alert);
        Task NotifyHeartbeatUpdatedAsync(Agent agent);
        Task NotifyMetricUpdatedAsync(MetricRecord metric);
        Task NotifySecurityEventCreatedAsync(SecurityEvent securityEvent);
        Task NotifyAgentStatusChangedAsync(Agent agent, string oldStatus, string newStatus);
        Task NotifyIncidentCreatedAsync(Incident incident);
        Task NotifyIncidentUpdatedAsync(Incident incident);
        Task NotifyIncidentAssignedAsync(Incident incident);
        Task NotifyIncidentStatusChangedAsync(Incident incident);
        Task NotifyIncidentClosedAsync(Incident incident);
        Task NotifyDashboardOverviewUpdatedAsync();
        Task NotifyResponseCreatedAsync(ResponseAction action);
        Task NotifyResponseStartedAsync(ResponseAction action);
        Task NotifyResponseUpdatedAsync(ResponseAction action);
        Task NotifyResponseCompletedAsync(ResponseAction action);
        Task NotifyResponseFailedAsync(ResponseAction action);
        Task NotifyAssetCreatedAsync(InfrastructureAsset asset);
        Task NotifyAssetUpdatedAsync(InfrastructureAsset asset);
        Task NotifyCollectorStatusChangedAsync(CollectorNode collector);
        Task NotifyPolicyUpdatedAsync(AgentPolicy policy);
        Task NotifyEnrollmentGeneratedAsync(EnrollmentToken token);
    }
}
