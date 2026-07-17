using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Services;
using OneSecurity.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace OneSecurity.Server.Realtime
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly IHubContext<NotificationHub, INotificationHub> _hubContext;
        private readonly ILogger<NotificationHubService> _logger;
        private readonly IOverviewService _overviewService;
        private readonly IServiceProvider _serviceProvider; // Thêm ServiceProvider để khởi tạo scope chạy nền

        public NotificationHubService(
            IHubContext<NotificationHub, INotificationHub> hubContext,
            ILogger<NotificationHubService> logger,
            IOverviewService overviewService,
            IServiceProvider serviceProvider)
        {
            _hubContext = hubContext;
            _logger = logger;
            _overviewService = overviewService;
            _serviceProvider = serviceProvider;
        }

        public async Task NotifyAlertCreatedAsync(Alert alert)
        {
            try
            {
                var payload = new AlertNotificationDto
                {
                    Id = alert.Id,
                    AgentId = alert.AgentId,
                    AgentHostname = alert.Agent?.Hostname ?? "Unknown Agent",
                    RuleName = alert.RuleName,
                    Severity = alert.Severity,
                    Title = alert.Title,
                    Message = alert.Message,
                    Category = alert.Category,
                    CreatedAt = alert.CreatedAt,
                    HospitalId = alert.Agent?.HospitalId ?? 1
                };

                var hospitalId = alert.Agent?.HospitalId ?? 1;

                // Phát tín hiệu cô lập theo nhóm bệnh viện và SuperAdmins
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").AlertCreated(payload);
                await _hubContext.Clients.Group("SuperAdmins").AlertCreated(payload);
                _logger.LogInformation("Broadcast Success: AlertCreated for AlertId: {AlertId} to Hospital_{HospitalId}", alert.Id, hospitalId);

                await NotifyDashboardOverviewUpdatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: AlertCreated for AlertId: {AlertId}", alert.Id);
            }
        }

        public async Task NotifyHeartbeatUpdatedAsync(Agent agent)
        {
            try
            {
                var payload = new HeartbeatNotificationDto
                {
                    AgentId = agent.Id,
                    AgentHostname = agent.Hostname,
                    IpAddress = agent.IpAddress,
                    Status = agent.Status,
                    LastSeenAt = agent.LastSeenAt,
                    HospitalId = agent.HospitalId ?? 1
                };

                var hospitalId = agent.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").HeartbeatUpdated(payload);
                await _hubContext.Clients.Group("SuperAdmins").HeartbeatUpdated(payload);
                _logger.LogInformation("Broadcast Success: HeartbeatUpdated for AgentId: {AgentId}", agent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: HeartbeatUpdated for AgentId: {AgentId}", agent.Id);
            }
        }

        public async Task NotifyMetricUpdatedAsync(MetricRecord metric)
        {
            try
            {
                var payload = new MetricNotificationDto
                {
                    Id = metric.Id,
                    AgentId = metric.AgentId,
                    AgentHostname = metric.Agent?.Hostname ?? "Unknown Agent",
                    CpuUsagePercent = metric.CpuUsagePercent,
                    RamUsagePercent = metric.RamUsagePercent,
                    DiskUsagePercent = metric.DiskUsagePercent,
                    NetworkInBytes = metric.NetworkInBytes,
                    NetworkOutBytes = metric.NetworkOutBytes,
                    Timestamp = metric.Timestamp,
                    HospitalId = metric.Agent?.HospitalId ?? 1
                };

                var hospitalId = metric.Agent?.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").MetricUpdated(payload);
                await _hubContext.Clients.Group("SuperAdmins").MetricUpdated(payload);
                _logger.LogInformation("Broadcast Success: MetricUpdated for MetricId: {MetricId}", metric.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: MetricUpdated for MetricId: {MetricId}", metric.Id);
            }
        }

        public async Task NotifySecurityEventCreatedAsync(SecurityEvent securityEvent)
        {
            try
            {
                var payload = new SecurityEventNotificationDto
                {
                    Id = securityEvent.Id,
                    EventId = securityEvent.EventId,
                    AgentId = securityEvent.AgentId,
                    AgentHostname = securityEvent.Agent?.Hostname ?? "Unknown Agent",
                    Category = securityEvent.Category,
                    Severity = securityEvent.Severity,
                    Source = securityEvent.Source,
                    Title = securityEvent.Title,
                    Details = securityEvent.Details,
                    Timestamp = securityEvent.Timestamp,
                    HospitalId = securityEvent.Agent?.HospitalId ?? 1
                };

                var hospitalId = securityEvent.Agent?.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").SecurityEventCreated(payload);
                await _hubContext.Clients.Group("SuperAdmins").SecurityEventCreated(payload);
                _logger.LogInformation("Broadcast Success: SecurityEventCreated for EventId: {EventId}", securityEvent.EventId);

                await NotifyDashboardOverviewUpdatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: SecurityEventCreated for EventId: {EventId}", securityEvent.EventId);
            }
        }

        public async Task NotifyAgentStatusChangedAsync(Agent agent, string oldStatus, string newStatus)
        {
            try
            {
                var payload = new AgentStatusNotificationDto
                {
                    AgentId = agent.Id,
                    AgentHostname = agent.Hostname,
                    IpAddress = agent.IpAddress,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    Timestamp = DateTime.UtcNow,
                    HospitalId = agent.HospitalId ?? 1
                };

                var hospitalId = agent.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").AgentStatusChanged(payload);
                await _hubContext.Clients.Group("SuperAdmins").AgentStatusChanged(payload);
                _logger.LogInformation("Broadcast Success: AgentStatusChanged for AgentId: {AgentId}", agent.Id);

                await NotifyDashboardOverviewUpdatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: AgentStatusChanged for AgentId: {AgentId}", agent.Id);
            }
        }

        public async Task NotifyIncidentCreatedAsync(Incident incident)
        {
            try
            {
                var payload = CreateIncidentPayload(incident);
                // Với Incident, tìm HospitalId của các Alert liên quan để gửi cho đúng nhóm
                var hospitalId = incident.Alerts?.FirstOrDefault()?.Agent?.HospitalId ?? 1;

                await _hubContext.Clients.Group($"Hospital_{hospitalId}").IncidentCreated(payload);
                await _hubContext.Clients.Group("SuperAdmins").IncidentCreated(payload);
                _logger.LogInformation("Broadcast Success: IncidentCreated for IncidentId: {IncidentId}", incident.Id);

                await NotifyDashboardOverviewUpdatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: IncidentCreated for IncidentId: {IncidentId}", incident.Id);
            }
        }

        public async Task NotifyIncidentUpdatedAsync(Incident incident)
        {
            try
            {
                var payload = CreateIncidentPayload(incident);
                var hospitalId = incident.Alerts?.FirstOrDefault()?.Agent?.HospitalId ?? 1;

                await _hubContext.Clients.Group($"Hospital_{hospitalId}").IncidentUpdated(payload);
                await _hubContext.Clients.Group("SuperAdmins").IncidentUpdated(payload);
                _logger.LogInformation("Broadcast Success: IncidentUpdated for IncidentId: {IncidentId}", incident.Id);

                await NotifyDashboardOverviewUpdatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: IncidentUpdated for IncidentId: {IncidentId}", incident.Id);
            }
        }

        public async Task NotifyIncidentAssignedAsync(Incident incident)
        {
            try
            {
                var payload = CreateIncidentPayload(incident);
                var hospitalId = incident.Alerts?.FirstOrDefault()?.Agent?.HospitalId ?? 1;

                await _hubContext.Clients.Group($"Hospital_{hospitalId}").IncidentAssigned(payload);
                await _hubContext.Clients.Group("SuperAdmins").IncidentAssigned(payload);
                _logger.LogInformation("Broadcast Success: IncidentAssigned for IncidentId: {IncidentId}", incident.Id);

                await NotifyDashboardOverviewUpdatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: IncidentAssigned for IncidentId: {IncidentId}", incident.Id);
            }
        }

        public async Task NotifyIncidentStatusChangedAsync(Incident incident)
        {
            try
            {
                var payload = CreateIncidentPayload(incident);
                var hospitalId = incident.Alerts?.FirstOrDefault()?.Agent?.HospitalId ?? 1;

                await _hubContext.Clients.Group($"Hospital_{hospitalId}").IncidentStatusChanged(payload);
                await _hubContext.Clients.Group("SuperAdmins").IncidentStatusChanged(payload);
                _logger.LogInformation("Broadcast Success: IncidentStatusChanged for IncidentId: {IncidentId}", incident.Id);

                await NotifyDashboardOverviewUpdatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: IncidentStatusChanged for IncidentId: {IncidentId}", incident.Id);
            }
        }

        public async Task NotifyIncidentClosedAsync(Incident incident)
        {
            try
            {
                var payload = CreateIncidentPayload(incident);
                var hospitalId = incident.Alerts?.FirstOrDefault()?.Agent?.HospitalId ?? 1;

                await _hubContext.Clients.Group($"Hospital_{hospitalId}").IncidentClosed(payload);
                await _hubContext.Clients.Group("SuperAdmins").IncidentClosed(payload);
                _logger.LogInformation("Broadcast Success: IncidentClosed for IncidentId: {IncidentId}", incident.Id);

                await NotifyDashboardOverviewUpdatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: IncidentClosed for IncidentId: {IncidentId}", incident.Id);
            }
        }

        public async Task NotifyDashboardOverviewUpdatedAsync()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<LocalAgentDbContext>();
                    var overviewService = scope.ServiceProvider.GetRequiredService<IOverviewService>();
                    var hierarchyCache = scope.ServiceProvider.GetRequiredService<IHospitalHierarchyCache>();

                    // 1. Phát dữ liệu Overview toàn cục cho SuperAdmins
                    dbContext.FilterOverride = null;
                    var globalOverview = await overviewService.GetLightweightOverviewAsync(null);
                    await _hubContext.Clients.Group("SuperAdmins").DashboardOverviewUpdated(globalOverview);

                    // 2. Phát dữ liệu cô lập dạng đệ quy cho từng Group Bệnh viện
                    var hospitals = await dbContext.Hospitals.ToListAsync();
                    foreach (var hosp in hospitals)
                    {
                        var permittedIds = hierarchyCache.GetDescendantIds(hosp.Id);
                        
                        // Override filter tạm thời cho tiến trình background
                        dbContext.FilterOverride = permittedIds; 
                        var hospOverview = await overviewService.GetLightweightOverviewAsync(null);
                        
                        await _hubContext.Clients.Group($"Hospital_{hosp.Id}").DashboardOverviewUpdated(hospOverview);
                    }
                }
                _logger.LogInformation("Broadcast Success: DashboardOverviewUpdated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: DashboardOverviewUpdated");
            }
        }

        private IncidentNotificationDto CreateIncidentPayload(Incident incident)
        {
            return new IncidentNotificationDto
            {
                Id = incident.Id,
                Title = incident.Title,
                Severity = incident.Severity.ToString(),
                Status = incident.Status.ToString(),
                AssignedUserId = incident.AssignedUserId,
                AssignedUserName = incident.AssignedUser?.UserName,
                AlertCount = incident.Alerts?.Count ?? 0,
                Timestamp = DateTime.UtcNow
            };
        }

        public async Task NotifyResponseCreatedAsync(ResponseAction action)
        {
            try
            {
                var payload = CreateResponsePayload(action);
                var hospitalId = action.Agent?.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").ResponseCreated(payload);
                await _hubContext.Clients.Group("SuperAdmins").ResponseCreated(payload);
                _logger.LogInformation("Broadcast Success: ResponseCreated for CorrelationId: {CorrelationId}", action.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: ResponseCreated");
            }
        }

        public async Task NotifyResponseStartedAsync(ResponseAction action)
        {
            try
            {
                var payload = CreateResponsePayload(action);
                var hospitalId = action.Agent?.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").ResponseStarted(payload);
                await _hubContext.Clients.Group("SuperAdmins").ResponseStarted(payload);
                _logger.LogInformation("Broadcast Success: ResponseStarted for CorrelationId: {CorrelationId}", action.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: ResponseStarted");
            }
        }

        public async Task NotifyResponseUpdatedAsync(ResponseAction action)
        {
            try
            {
                var payload = CreateResponsePayload(action);
                var hospitalId = action.Agent?.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").ResponseUpdated(payload);
                await _hubContext.Clients.Group("SuperAdmins").ResponseUpdated(payload);
                _logger.LogInformation("Broadcast Success: ResponseUpdated for CorrelationId: {CorrelationId}", action.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: ResponseUpdated");
            }
        }

        public async Task NotifyResponseCompletedAsync(ResponseAction action)
        {
            try
            {
                var payload = CreateResponsePayload(action);
                var hospitalId = action.Agent?.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").ResponseCompleted(payload);
                await _hubContext.Clients.Group("SuperAdmins").ResponseCompleted(payload);
                _logger.LogInformation("Broadcast Success: ResponseCompleted for CorrelationId: {CorrelationId}", action.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: ResponseCompleted");
            }
        }

        public async Task NotifyResponseFailedAsync(ResponseAction action)
        {
            try
            {
                var payload = CreateResponsePayload(action);
                var hospitalId = action.Agent?.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").ResponseFailed(payload);
                await _hubContext.Clients.Group("SuperAdmins").ResponseFailed(payload);
                _logger.LogInformation("Broadcast Success: ResponseFailed for CorrelationId: {CorrelationId}", action.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: ResponseFailed");
            }
        }

        public async Task NotifyAssetCreatedAsync(InfrastructureAsset asset)
        {
            try
            {
                var hospitalId = asset.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").AssetCreated(asset);
                await _hubContext.Clients.Group("SuperAdmins").AssetCreated(asset);
                _logger.LogInformation("Broadcast Success: AssetCreated for AssetId: {AssetId}", asset.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: AssetCreated");
            }
        }

        public async Task NotifyAssetUpdatedAsync(InfrastructureAsset asset)
        {
            try
            {
                var hospitalId = asset.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").AssetUpdated(asset);
                await _hubContext.Clients.Group("SuperAdmins").AssetUpdated(asset);
                _logger.LogInformation("Broadcast Success: AssetUpdated for AssetId: {AssetId}", asset.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: AssetUpdated");
            }
        }

        public async Task NotifyCollectorStatusChangedAsync(CollectorNode collector)
        {
            try
            {
                // Collector là global -> Broadcast cho tất cả
                await _hubContext.Clients.All.CollectorStatusChanged(collector);
                _logger.LogInformation("Broadcast Success: CollectorStatusChanged for CollectorId: {CollectorId}", collector.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: CollectorStatusChanged");
            }
        }

        public async Task NotifyPolicyUpdatedAsync(AgentPolicy policy)
        {
            try
            {
                // Policy là global -> Broadcast cho tất cả
                await _hubContext.Clients.All.PolicyUpdated(policy);
                _logger.LogInformation("Broadcast Success: PolicyUpdated for PolicyId: {PolicyId}", policy.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: PolicyUpdated");
            }
        }

        public async Task NotifyEnrollmentGeneratedAsync(EnrollmentToken token)
        {
            try
            {
                var hospitalId = token.Asset?.HospitalId ?? 1;
                await _hubContext.Clients.Group($"Hospital_{hospitalId}").EnrollmentGenerated(token);
                await _hubContext.Clients.Group("SuperAdmins").EnrollmentGenerated(token);
                _logger.LogInformation("Broadcast Success: EnrollmentGenerated for TokenId: {TokenId}", token.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast Failure: EnrollmentGenerated");
            }
        }

        private ResponseActionNotificationDto CreateResponsePayload(ResponseAction action)
        {
            return new ResponseActionNotificationDto
            {
                Id = action.Id,
                IncidentId = action.IncidentId,
                AgentId = action.AgentId,
                ActionType = action.ActionType.ToString(),
                Status = action.Status.ToString(),
                CorrelationId = action.CorrelationId,
                ResultMessage = action.ResultMessage,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}