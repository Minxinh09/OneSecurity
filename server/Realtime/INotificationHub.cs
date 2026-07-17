using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Realtime
{
    public interface INotificationHub
    {
        Task AlertCreated(AlertNotificationDto payload);
        Task HeartbeatUpdated(HeartbeatNotificationDto payload);
        Task MetricUpdated(MetricNotificationDto payload);
        Task SecurityEventCreated(SecurityEventNotificationDto payload);
        Task AgentStatusChanged(AgentStatusNotificationDto payload);
        Task IncidentCreated(IncidentNotificationDto payload);
        Task IncidentUpdated(IncidentNotificationDto payload);
        Task IncidentAssigned(IncidentNotificationDto payload);
        Task IncidentStatusChanged(IncidentNotificationDto payload);
        Task IncidentClosed(IncidentNotificationDto payload);
        Task DashboardOverviewUpdated(DashboardOverviewUpdatedDto payload);
        Task ResponseCreated(ResponseActionNotificationDto payload);
        Task ResponseStarted(ResponseActionNotificationDto payload);
        Task ResponseUpdated(ResponseActionNotificationDto payload);
        Task ResponseCompleted(ResponseActionNotificationDto payload);
        Task ResponseFailed(ResponseActionNotificationDto payload);
        Task AssetCreated(InfrastructureAsset asset);
        Task AssetUpdated(InfrastructureAsset asset);
        Task CollectorStatusChanged(CollectorNode collector);
        Task PolicyUpdated(AgentPolicy policy);
        Task EnrollmentGenerated(EnrollmentToken token);
    }
}
