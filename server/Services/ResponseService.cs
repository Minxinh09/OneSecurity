using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Realtime;
using OneSecurity.Server.Repositories;
using Microsoft.EntityFrameworkCore;

namespace OneSecurity.Server.Services
{
    public class ResponseService : IResponseService
    {
        private readonly IResponseRepository _responseRepository;
        private readonly IAgentRepository _agentRepository;
        private readonly IAuditService _auditService;
        private readonly INotificationHubService _notificationHubService;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LocalAgentDbContext _context;

        public ResponseService(
            IResponseRepository responseRepository,
            IAgentRepository agentRepository,
            IAuditService auditService,
            INotificationHubService notificationHubService,
            ICommandDispatcher commandDispatcher,
            UserManager<ApplicationUser> userManager,
            LocalAgentDbContext context)
        {
            _responseRepository = responseRepository;
            _agentRepository = agentRepository;
            _auditService = auditService;
            _notificationHubService = notificationHubService;
            _commandDispatcher = commandDispatcher;
            _userManager = userManager;
            _context = context;
        }

        public async Task<ResponseActionDto> RequestActionAsync(RequestResponseActionRequest request, string userId)
        {
            // 1. Verify user role
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            var isOperator = await _userManager.IsInRoleAsync(user, "Operator");
            var isSecOp = await _userManager.IsInRoleAsync(user, "SecurityOperator");
            
            // Viewers are read-only and cannot request responses
            if (!isAdmin && !isOperator && !isSecOp)
            {
                await _auditService.LogAsync(
                    action: "Response Action Unauthorized",
                    resourceType: AuditResourceType.User,
                    entityId: request.AgentId,
                    description: $"User '{user.UserName}' attempted unauthorized execution of response '{request.ActionType}' on agent '{request.AgentId}'",
                    success: false,
                    statusCode: 403,
                    severity: AuditSeverity.Critical,
                    userNameOverride: user.UserName
                );
                throw new UnauthorizedAccessException("Viewer role is read-only and cannot execute responses.");
            }

            // 2. Validate Incident and Agent
            var incident = await _context.Incidents
                .IgnoreQueryFilters()
                .Include(i => i.Alerts)
                    .ThenInclude(a => a.Agent)
                .FirstOrDefaultAsync(i => i.Id == request.IncidentId);

            if (incident == null) throw new ArgumentException($"Incident with ID {request.IncidentId} not found.");
            
            var permittedHospitalIds = _context.PermittedHospitalIds;
            bool hasIncidentAccess = isAdmin || 
                                     (incident.AssignedUserId == userId) || 
                                     (permittedHospitalIds != null && incident.Alerts.Any(al => al.AgentId != null && al.Agent != null && al.Agent.HospitalId.HasValue && permittedHospitalIds.Contains(al.Agent.HospitalId.Value)));
                                     
            if (!hasIncidentAccess)
            {
                throw new UnauthorizedAccessException("You do not have access to this incident.");
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                throw new InvalidOperationException("Cannot trigger response actions on a closed incident.");
            }

            var agent = await _context.Agents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == request.AgentId);

            if (agent == null) throw new ArgumentException($"Agent with ID {request.AgentId} not found.");

            bool hasAgentAccess = isAdmin ||
                                  (agent.HospitalId.HasValue && permittedHospitalIds != null && permittedHospitalIds.Contains(agent.HospitalId.Value)) ||
                                  incident.Alerts.Any(a => a.AgentId == request.AgentId);
                                  
            if (!hasAgentAccess)
            {
                throw new UnauthorizedAccessException("You do not have access to this agent.");
            }

            var rawActionType = request.ActionType;
            if (string.Equals(rawActionType, "RestartSqlServer", StringComparison.OrdinalIgnoreCase))
            {
                rawActionType = "RestartSQL";
            }
            else if (string.Equals(rawActionType, "SyncConfig", StringComparison.OrdinalIgnoreCase))
            {
                rawActionType = "SyncConfiguration";
            }
            else if (string.Equals(rawActionType, "Reboot", StringComparison.OrdinalIgnoreCase))
            {
                rawActionType = "Restart";
            }

            if (!Enum.TryParse<ResponseActionType>(rawActionType, true, out var actionType))
            {
                throw new ArgumentException($"Invalid response action type: {request.ActionType}");
            }

            if (actionType != ResponseActionType.Restart &&
                actionType != ResponseActionType.CollectDiagnostics &&
                actionType != ResponseActionType.RunScan &&
                actionType != ResponseActionType.SyncConfiguration &&
                actionType != ResponseActionType.CollectLogs &&
                actionType != ResponseActionType.RestartAgent &&
                actionType != ResponseActionType.RestartCollector &&
                actionType != ResponseActionType.RestartIIS &&
                actionType != ResponseActionType.RestartSQL &&
                actionType != ResponseActionType.Shutdown)
            {
                throw new InvalidOperationException($"Action type '{actionType}' is not supported or is disabled for security reasons.");
            }

            bool isDangerous = !isAdmin;
            var status = isAdmin ? ResponseStatus.Queued : ResponseStatus.Pending;
            string? approvedByUserId = isAdmin ? userId : null;

            var correlationId = Guid.NewGuid().ToString("N");

            var action = new ResponseAction
            {
                IncidentId = request.IncidentId,
                AgentId = request.AgentId,
                ActionType = actionType,
                Status = status,
                RequestedByUserId = userId,
                ApprovedByUserId = approvedByUserId,
                CorrelationId = correlationId,
                Metadata = request.Metadata,
                RequestedAt = DateTime.UtcNow
            };

            await _responseRepository.AddAsync(action);
            await _responseRepository.SaveChangesAsync();

            // Load navigations
            var reloaded = await _responseRepository.GetByIdAsync(action.Id);

            // Audit Log
            await _auditService.LogAsync(
                action: "Response Requested",
                resourceType: AuditResourceType.Agent,
                entityId: reloaded!.Id.ToString(),
                description: $"Requested response '{actionType}' on host '{reloaded.Agent?.Hostname}'. Status: {status}",
                success: true,
                statusCode: status == ResponseStatus.Pending ? 202 : 201,
                severity: isDangerous ? AuditSeverity.Warning : AuditSeverity.Information,
                userNameOverride: user.UserName
            );

            // Broadcast SignalR
            await _notificationHubService.NotifyResponseCreatedAsync(reloaded);

            if (status == ResponseStatus.Queued)
            {
                // Dispatch command asynchronously to Collector
                _ = Task.Run(async () => {
                    var success = await _commandDispatcher.DispatchAsync(reloaded);
                    if (!success)
                    {
                        reloaded.Status = ResponseStatus.Failed;
                        reloaded.ResultMessage = "Failed to dispatch command to Collector.";
                        reloaded.CompletedAt = DateTime.UtcNow;
                        _responseRepository.Update(reloaded);
                        await _responseRepository.SaveChangesAsync();

                        await _notificationHubService.NotifyResponseFailedAsync(reloaded);
                        await _auditService.LogAsync(
                            action: "Response Failed",
                            resourceType: AuditResourceType.Agent,
                            entityId: reloaded.Id.ToString(),
                            description: $"Failed to dispatch response '{actionType}' on host '{reloaded.Agent?.Hostname}': Collector unreachable.",
                            success: false,
                            statusCode: 502,
                            severity: AuditSeverity.Warning,
                            userNameOverride: "System"
                        );
                    }
                });
            }

            return MapToDto(reloaded);
        }

        public async Task<ResponseActionDto?> ApproveActionAsync(long actionId, string adminUserId)
        {
            var adminUser = await _userManager.FindByIdAsync(adminUserId);
            if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, "Administrator"))
            {
                throw new UnauthorizedAccessException("Only Administrators can approve response actions.");
            }

            var action = await _responseRepository.GetByIdAsync(actionId);
            if (action == null) return null;

            if (action.Status != ResponseStatus.Pending)
            {
                throw new InvalidOperationException("Only Pending actions can be approved.");
            }

            action.Status = ResponseStatus.Queued;
            action.ApprovedByUserId = adminUserId;
            action.RequestedAt = DateTime.UtcNow; // Reset requested at to dispatch time
            
            _responseRepository.Update(action);
            await _responseRepository.SaveChangesAsync();

            // Audit
            await _auditService.LogAsync(
                action: "Response Approved",
                resourceType: AuditResourceType.Agent,
                entityId: action.Id.ToString(),
                description: $"Approved response '{action.ActionType}' on host '{action.Agent?.Hostname}'",
                success: true,
                statusCode: 200,
                severity: AuditSeverity.Information,
                userNameOverride: adminUser.UserName
            );

            await _notificationHubService.NotifyResponseUpdatedAsync(action);

            // Dispatch command
            _ = Task.Run(async () => {
                var success = await _commandDispatcher.DispatchAsync(action);
                if (!success)
                {
                    action.Status = ResponseStatus.Failed;
                    action.ResultMessage = "Failed to dispatch command to Collector.";
                    action.CompletedAt = DateTime.UtcNow;
                    _responseRepository.Update(action);
                    await _responseRepository.SaveChangesAsync();

                    await _notificationHubService.NotifyResponseFailedAsync(action);
                }
            });

            return MapToDto(action);
        }

        public async Task<ResponseActionDto?> CancelActionAsync(long actionId, string adminUserId)
        {
            var adminUser = await _userManager.FindByIdAsync(adminUserId);
            if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, "Administrator"))
            {
                throw new UnauthorizedAccessException("Only Administrators can cancel response actions.");
            }

            var action = await _responseRepository.GetByIdAsync(actionId);
            if (action == null) return null;

            if (action.Status != ResponseStatus.Pending && action.Status != ResponseStatus.Queued)
            {
                throw new InvalidOperationException("Only Pending or Queued actions can be cancelled.");
            }

            action.Status = ResponseStatus.Cancelled;
            action.CompletedAt = DateTime.UtcNow;
            action.ResultMessage = "Cancelled by Administrator.";
            
            _responseRepository.Update(action);
            await _responseRepository.SaveChangesAsync();

            // Audit
            await _auditService.LogAsync(
                action: "Response Cancelled",
                resourceType: AuditResourceType.Agent,
                entityId: action.Id.ToString(),
                description: $"Cancelled response '{action.ActionType}' on host '{action.Agent?.Hostname}'",
                success: true,
                statusCode: 200,
                severity: AuditSeverity.Information,
                userNameOverride: adminUser.UserName
            );

            await _notificationHubService.NotifyResponseUpdatedAsync(action);

            return MapToDto(action);
        }

        public async Task<ResponseActionDto?> GetByIdAsync(long actionId)
        {
            var action = await _responseRepository.GetByIdAsync(actionId);
            return action != null ? MapToDto(action) : null;
        }

        public async Task<List<ResponseActionDto>> GetByIncidentIdAsync(long incidentId)
        {
            var items = await _responseRepository.GetByIncidentIdAsync(incidentId);
            return items.Select(MapToDto).ToList();
        }

        public async Task<ResponseActionListResponse> GetPagedAsync(
            int page, 
            int pageSize, 
            string? status = null, 
            string? actionType = null, 
            string? agentId = null)
        {
            var (items, totalCount) = await _responseRepository.GetPagedAsync(page, pageSize, status, actionType, agentId);
            return new ResponseActionListResponse
            {
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = items.Select(MapToDto).ToList()
            };
        }

        public async Task<bool> UpdateExecutionStatusAsync(string correlationId, string status, string? message)
        {
            var action = await _responseRepository.GetByCorrelationIdAsync(correlationId);
            if (action == null) return false;

            if (!Enum.TryParse<ResponseStatus>(status, true, out var statusEnum))
            {
                return false;
            }

            action.Status = statusEnum;

            if (statusEnum == ResponseStatus.Executing)
            {
                action.StartedAt = DateTime.UtcNow;
                _responseRepository.Update(action);
                await _responseRepository.SaveChangesAsync();
                
                await _notificationHubService.NotifyResponseStartedAsync(action);
                
                await _auditService.LogAsync(
                    action: "Response Started",
                    resourceType: AuditResourceType.Agent,
                    entityId: action.Id.ToString(),
                    description: $"Started execution of response '{action.ActionType}' on host '{action.Agent?.Hostname}'",
                    success: true,
                    statusCode: 200,
                    severity: AuditSeverity.Information,
                    userNameOverride: "System"
                );
            }
            else if (statusEnum == ResponseStatus.Succeeded)
            {
                action.CompletedAt = DateTime.UtcNow;
                action.ResultMessage = message ?? "Succeeded.";
                action.Output = message;
                _responseRepository.Update(action);
                await _responseRepository.SaveChangesAsync();

                await _notificationHubService.NotifyResponseCompletedAsync(action);

                await _auditService.LogAsync(
                    action: "Response Completed",
                    resourceType: AuditResourceType.Agent,
                    entityId: action.Id.ToString(),
                    description: $"Completed response '{action.ActionType}' on host '{action.Agent?.Hostname}': {action.ResultMessage}",
                    success: true,
                    statusCode: 200,
                    severity: AuditSeverity.Information,
                    userNameOverride: "System"
                );
            }
            else if (statusEnum == ResponseStatus.Failed)
            {
                action.CompletedAt = DateTime.UtcNow;
                action.ResultMessage = message ?? "Failed.";
                action.ErrorMessage = message;
                _responseRepository.Update(action);
                await _responseRepository.SaveChangesAsync();

                await _notificationHubService.NotifyResponseFailedAsync(action);

                await _auditService.LogAsync(
                    action: "Response Failed",
                    resourceType: AuditResourceType.Agent,
                    entityId: action.Id.ToString(),
                    description: $"Failed response '{action.ActionType}' on host '{action.Agent?.Hostname}': {action.ResultMessage}",
                    success: false,
                    statusCode: 500,
                    severity: AuditSeverity.Warning,
                    userNameOverride: "System"
                );
            }

            return true;
        }

        private ResponseActionDto MapToDto(ResponseAction action)
        {
            return new ResponseActionDto
            {
                Id = action.Id,
                IncidentId = action.IncidentId,
                AgentId = action.AgentId,
                AgentHostname = action.Agent?.Hostname ?? "Unknown Agent",
                ActionType = action.ActionType.ToString(),
                Status = action.Status.ToString(),
                RequestedByUserId = action.RequestedByUserId,
                RequestedByUserName = action.RequestedByUser?.UserName ?? "System",
                ApprovedByUserId = action.ApprovedByUserId,
                ApprovedByUserName = action.ApprovedByUser?.UserName,
                RequestedAt = action.RequestedAt,
                StartedAt = action.StartedAt,
                CompletedAt = action.CompletedAt,
                ResultMessage = action.ResultMessage,
                CorrelationId = action.CorrelationId,
                Metadata = action.Metadata,
                Parameters = action.Parameters,
                ErrorMessage = action.ErrorMessage,
                Output = action.Output,
                HospitalId = action.HospitalId,
                CreatedBy = action.CreatedBy
            };
        }
    }
}
