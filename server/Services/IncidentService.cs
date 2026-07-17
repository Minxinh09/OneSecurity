using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Realtime;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly IIncidentRepository _incidentRepository;
        private readonly LocalAgentDbContext _context; // Inject directly to fetch Alerts and check users
        private readonly INotificationHubService _notificationHubService;
        private readonly IAuditService _auditService;
        private readonly UserManager<ApplicationUser> _userManager;

        public IncidentService(
            IIncidentRepository incidentRepository,
            LocalAgentDbContext context,
            INotificationHubService notificationHubService,
            IAuditService auditService,
            UserManager<ApplicationUser> userManager)
        {
            _incidentRepository = incidentRepository;
            _context = context;
            _notificationHubService = notificationHubService;
            _auditService = auditService;
            _userManager = userManager;
        }

        public async Task<IncidentDetailDto> CreateAsync(CreateIncidentRequest request, string createdByUserId)
        {
            var creator = await _userManager.FindByIdAsync(createdByUserId);
            var creatorName = creator?.UserName ?? "System";

            var incident = new Incident
            {
                Title = request.Title,
                Description = request.Description,
                Status = IncidentStatus.New,
                Severity = IncidentSeverity.Low, // Will be recalculated
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            // Link alerts if provided
            if (request.AlertIds != null && request.AlertIds.Count > 0)
            {
                foreach (var alertId in request.AlertIds)
                {
                    var alert = await _context.Alerts.FindAsync(alertId);
                    if (alert == null)
                    {
                        throw new ArgumentException($"Alert with ID {alertId} not found.");
                    }
                    if (alert.IncidentId.HasValue)
                    {
                        throw new InvalidOperationException($"Alert {alertId} is already assigned to Incident {alert.IncidentId}.");
                    }
                    incident.Alerts.Add(alert);
                }
            }

            RecalculateSeverity(incident);
            await _incidentRepository.AddAsync(incident);
            await _incidentRepository.SaveChangesAsync();

            // Reload to fetch navigate properties
            var reloaded = await _incidentRepository.GetByIdAsync(incident.Id);
            
            await _notificationHubService.NotifyIncidentCreatedAsync(reloaded!);
            await _auditService.LogAsync(
                action: "Incident Created",
                resourceType: AuditResourceType.System,
                entityId: reloaded!.Id.ToString(),
                description: $"Created incident {reloaded.Id}: '{reloaded.Title}'",
                success: true,
                statusCode: 201,
                severity: AuditSeverity.Information,
                userNameOverride: creatorName
            );

            return MapToDetailDto(reloaded);
        }

        public async Task<IncidentDetailDto> AssignAsync(long id, AssignIncidentRequest request, string currentUserId)
        {
            var incident = await _incidentRepository.GetByIdAsync(id);
            if (incident == null)
            {
                throw new ArgumentException($"Incident with ID {id} not found.");
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                throw new InvalidOperationException("Cannot assign a closed incident.");
            }

            var assigner = await _userManager.FindByIdAsync(currentUserId);
            var assignerName = assigner?.UserName ?? "System";

            string? assignedUserName = null;
            if (!string.IsNullOrEmpty(request.AssignedUserId))
            {
                var user = await _userManager.FindByIdAsync(request.AssignedUserId);
                if (user == null)
                {
                    throw new ArgumentException($"User with ID {request.AssignedUserId} not found.");
                }
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Count == 0 || roles.Contains("Viewer"))
                {
                    throw new InvalidOperationException("Cannot assign incidents to a read-only Viewer.");
                }
                assignedUserName = user.UserName;
                incident.AssignedUserId = request.AssignedUserId;
                incident.AssignedAt = DateTime.UtcNow;

                // Auto transition to Assigned if currently New
                if (incident.Status == IncidentStatus.New)
                {
                    incident.Status = IncidentStatus.Assigned;
                }
            }
            else
            {
                incident.AssignedUserId = null;
                incident.AssignedAt = null;
            }

            incident.UpdatedAt = DateTime.UtcNow;
            _incidentRepository.Update(incident);
            await _incidentRepository.SaveChangesAsync();

            await _notificationHubService.NotifyIncidentAssignedAsync(incident);
            await _auditService.LogAsync(
                action: "Incident Assigned",
                resourceType: AuditResourceType.System,
                entityId: incident.Id.ToString(),
                description: string.IsNullOrEmpty(assignedUserName) 
                    ? $"Unassigned incident {incident.Id}" 
                    : $"Assigned incident {incident.Id} to user '{assignedUserName}'",
                success: true,
                statusCode: 200,
                severity: AuditSeverity.Information,
                userNameOverride: assignerName
            );

            return MapToDetailDto(incident);
        }

        public async Task<IncidentDetailDto> UpdateStatusAsync(long id, UpdateIncidentStatusRequest request, string currentUserId)
        {
            var incident = await _incidentRepository.GetByIdAsync(id);
            if (incident == null)
            {
                throw new ArgumentException($"Incident with ID {id} not found.");
            }

            if (!Enum.TryParse<IncidentStatus>(request.Status, true, out var nextStatus))
            {
                throw new ArgumentException($"Invalid status value: {request.Status}");
            }

            ValidateTransition(incident.Status, nextStatus);

            var operatorUser = await _userManager.FindByIdAsync(currentUserId);
            var operatorName = operatorUser?.UserName ?? "System";

            var oldStatus = incident.Status;
            incident.Status = nextStatus;
            incident.UpdatedAt = DateTime.UtcNow;

            string auditAction = "Incident Status Changed";
            var auditSeverity = AuditSeverity.Information;

            if (nextStatus == IncidentStatus.Resolved)
            {
                incident.ResolvedAt = DateTime.UtcNow;
                incident.ResolvedByUserId = currentUserId;
                auditAction = "Incident Resolved";
            }
            else if (nextStatus == IncidentStatus.FalsePositive)
            {
                incident.ResolvedAt = DateTime.UtcNow;
                incident.ResolvedByUserId = currentUserId;
                auditAction = "Incident False Positive";
                auditSeverity = AuditSeverity.Warning;
            }
            else if (nextStatus == IncidentStatus.Closed)
            {
                incident.ClosedAt = DateTime.UtcNow;
                incident.ClosedByUserId = currentUserId;
                auditAction = "Incident Closed";
            }

            _incidentRepository.Update(incident);
            await _incidentRepository.SaveChangesAsync();

            if (nextStatus == IncidentStatus.Closed)
            {
                await _notificationHubService.NotifyIncidentClosedAsync(incident);
            }
            else
            {
                await _notificationHubService.NotifyIncidentStatusChangedAsync(incident);
            }

            await _auditService.LogAsync(
                action: auditAction,
                resourceType: AuditResourceType.System,
                entityId: incident.Id.ToString(),
                description: $"Changed incident {incident.Id} status from {oldStatus} to {nextStatus}",
                success: true,
                statusCode: 200,
                severity: auditSeverity,
                userNameOverride: operatorName
            );

            return MapToDetailDto(incident);
        }

        public async Task<IncidentDetailDto> LinkAlertsAsync(long id, LinkAlertsRequest request, string currentUserId)
        {
            var incident = await _incidentRepository.GetByIdAsync(id);
            if (incident == null)
            {
                throw new ArgumentException($"Incident with ID {id} not found.");
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                throw new InvalidOperationException("Cannot link alerts to a closed incident.");
            }

            var operatorUser = await _userManager.FindByIdAsync(currentUserId);
            var operatorName = operatorUser?.UserName ?? "System";

            var linkedAlertIds = new List<long>();
            foreach (var alertId in request.AlertIds)
            {
                var alert = await _context.Alerts.FindAsync(alertId);
                if (alert == null)
                {
                    throw new ArgumentException($"Alert with ID {alertId} not found.");
                }
                if (alert.IncidentId.HasValue)
                {
                    if (alert.IncidentId.Value == id) continue; // Already linked to this incident
                    throw new InvalidOperationException($"Alert {alertId} is already assigned to Incident {alert.IncidentId}.");
                }
                incident.Alerts.Add(alert);
                linkedAlertIds.Add(alertId);
            }

            if (linkedAlertIds.Count > 0)
            {
                RecalculateSeverity(incident);
                incident.UpdatedAt = DateTime.UtcNow;
                _incidentRepository.Update(incident);
                await _incidentRepository.SaveChangesAsync();

                await _notificationHubService.NotifyIncidentUpdatedAsync(incident);
                await _auditService.LogAsync(
                    action: "Link Alert",
                    resourceType: AuditResourceType.System,
                    entityId: incident.Id.ToString(),
                    description: $"Linked alerts [{string.Join(", ", linkedAlertIds)}] to incident {incident.Id}",
                    success: true,
                    statusCode: 200,
                    severity: AuditSeverity.Information,
                    userNameOverride: operatorName
                );
            }

            return MapToDetailDto(incident);
        }

        public async Task<IncidentDetailDto> UnlinkAlertAsync(long id, long alertId, string currentUserId)
        {
            var incident = await _incidentRepository.GetByIdAsync(id);
            if (incident == null)
            {
                throw new ArgumentException($"Incident with ID {id} not found.");
            }

            if (incident.Status == IncidentStatus.Closed)
            {
                throw new InvalidOperationException("Cannot unlink alerts from a closed incident.");
            }

            var alert = incident.Alerts.FirstOrDefault(a => a.Id == alertId);
            if (alert == null)
            {
                throw new ArgumentException($"Alert with ID {alertId} is not linked to incident {id}.");
            }

            var operatorUser = await _userManager.FindByIdAsync(currentUserId);
            var operatorName = operatorUser?.UserName ?? "System";

            incident.Alerts.Remove(alert);
            alert.IncidentId = null;

            RecalculateSeverity(incident);
            incident.UpdatedAt = DateTime.UtcNow;
            _incidentRepository.Update(incident);
            await _incidentRepository.SaveChangesAsync();

            await _notificationHubService.NotifyIncidentUpdatedAsync(incident);
            await _auditService.LogAsync(
                action: "Unlink Alert",
                resourceType: AuditResourceType.System,
                entityId: incident.Id.ToString(),
                description: $"Unlinked alert {alertId} from incident {incident.Id}",
                success: true,
                statusCode: 200,
                severity: AuditSeverity.Information,
                userNameOverride: operatorName
            );

            return MapToDetailDto(incident);
        }

        #region Helpers

        public void RecalculateSeverity(Incident incident)
        {
            if (incident.Alerts == null || incident.Alerts.Count == 0)
            {
                incident.Severity = IncidentSeverity.Low;
                return;
            }

            var maxSeverity = IncidentSeverity.Low;
            foreach (var alert in incident.Alerts)
            {
                var alertSeverityStr = alert.Severity.ToLower();
                IncidentSeverity currentAlertSeverity;

                if (alertSeverityStr == "critical")
                    currentAlertSeverity = IncidentSeverity.Critical;
                else if (alertSeverityStr == "high")
                    currentAlertSeverity = IncidentSeverity.High;
                else if (alertSeverityStr == "warning" || alertSeverityStr == "medium")
                    currentAlertSeverity = IncidentSeverity.Medium;
                else
                    currentAlertSeverity = IncidentSeverity.Low;

                if (currentAlertSeverity > maxSeverity)
                {
                    maxSeverity = currentAlertSeverity;
                }
            }
            incident.Severity = maxSeverity;
        }

        public bool CanTransition(IncidentStatus current, IncidentStatus next)
        {
            if (current == next) return true;

            return current switch
            {
                IncidentStatus.New => next == IncidentStatus.Assigned,
                IncidentStatus.Assigned => next == IncidentStatus.Investigating,
                IncidentStatus.Investigating => next == IncidentStatus.Resolved || next == IncidentStatus.FalsePositive,
                IncidentStatus.Resolved => next == IncidentStatus.Closed,
                IncidentStatus.FalsePositive => next == IncidentStatus.Closed,
                IncidentStatus.Closed => false, // Cannot reopen
                _ => false
            };
        }

        public void ValidateTransition(IncidentStatus current, IncidentStatus next)
        {
            if (!CanTransition(current, next))
            {
                throw new InvalidOperationException($"Invalid status transition from {current} to {next}.");
            }
        }

        private IncidentDetailDto MapToDetailDto(Incident incident)
        {
            var resolvedBy = !string.IsNullOrEmpty(incident.ResolvedByUserId) 
                ? _context.Users.FirstOrDefault(u => u.Id == incident.ResolvedByUserId)?.UserName 
                : null;
                
            var closedBy = !string.IsNullOrEmpty(incident.ClosedByUserId) 
                ? _context.Users.FirstOrDefault(u => u.Id == incident.ClosedByUserId)?.UserName 
                : null;

            return new IncidentDetailDto
            {
                Id = incident.Id,
                Title = incident.Title,
                Description = incident.Description,
                Severity = incident.Severity.ToString(),
                Status = incident.Status.ToString(),
                AssignedUserId = incident.AssignedUserId,
                AssignedUserName = incident.AssignedUser?.UserName,
                AssignedAt = incident.AssignedAt,
                CreatedAt = incident.CreatedAt,
                UpdatedAt = incident.UpdatedAt,
                AlertCount = incident.Alerts.Count,
                CreatedBy = incident.CreatedBy?.UserName,
                ResolvedAt = incident.ResolvedAt,
                ResolvedBy = resolvedBy,
                ClosedAt = incident.ClosedAt,
                ClosedBy = closedBy,
                Alerts = incident.Alerts.Select(a => new RecentAlertDto
                {
                    Id = a.Id,
                    AgentId = a.AgentId,
                    AgentHostname = a.Agent?.Hostname,
                    RuleName = a.RuleName,
                    Severity = a.Severity,
                    Title = a.Title,
                    Message = a.Message,
                    Category = a.Category,
                    CreatedAt = a.CreatedAt,
                    IsAcknowledged = a.IsAcknowledged
                }).ToList()
            };
        }

        public async Task CorrelateAlertAsync(Alert alert)
        {
            // Ensure alert navigation properties are loaded
            var agent = alert.Agent ?? await _context.Agents.FindAsync(alert.AgentId);
            var triggerEvent = alert.TriggerEvent ?? await _context.SecurityEvents.FindAsync(alert.TriggerEventId);
            
            var ruleName = alert.RuleName;
            var hostname = agent?.Hostname ?? "Unknown Host";
            
            // Extract Username, IP, Source, EventID
            string? username = null;
            string? ipAddress = null;
            string? source = triggerEvent?.Source;
            string? eventId = triggerEvent?.EventId;

            if (triggerEvent != null)
            {
                if (!string.IsNullOrEmpty(triggerEvent.RawData))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(triggerEvent.RawData);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("ClientIP", out var ipProp)) ipAddress = ipProp.GetString();
                        else if (root.TryGetProperty("SourceAddress", out var saProp)) ipAddress = saProp.GetString();

                        if (root.TryGetProperty("UserName", out var userProp)) username = userProp.GetString();
                        else if (root.TryGetProperty("AccountName", out var accProp)) username = accProp.GetString();
                    }
                    catch { /* ignore parsing errors */ }
                }

                if (string.IsNullOrEmpty(ipAddress) && !string.IsNullOrEmpty(triggerEvent.Details))
                {
                    var ipRegex = new System.Text.RegularExpressions.Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                    var match = ipRegex.Match(triggerEvent.Details);
                    if (match.Success) ipAddress = match.Value;
                }

                if (string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(triggerEvent.Details))
                {
                    var userRegex = new System.Text.RegularExpressions.Regex(@"Account Name:\s*([^\s,]+)");
                    var match = userRegex.Match(triggerEvent.Details);
                    if (match.Success) username = match.Groups[1].Value;
                }
            }

            // Find active incidents (New, Assigned, Investigating)
            var activeIncidents = await _context.Incidents
                .Include(i => i.Alerts)
                .ThenInclude(a => a.TriggerEvent)
                .Where(i => i.Status == IncidentStatus.New || 
                            i.Status == IncidentStatus.Assigned || 
                            i.Status == IncidentStatus.Investigating)
                .ToListAsync();

            Incident? matchedIncident = null;

            foreach (var incident in activeIncidents)
            {
                bool correlates = incident.Alerts.Any(a => 
                {
                    // Match RuleId
                    if (a.RuleId == alert.RuleId) return true;
                    
                    // Match Agent
                    if (a.AgentId == alert.AgentId) return true;
                    
                    // Match IP
                    if (!string.IsNullOrEmpty(ipAddress) && (a.Message.Contains(ipAddress) || (a.TriggerEvent != null && a.TriggerEvent.RawData.Contains(ipAddress)))) return true;

                    // Match User
                    if (!string.IsNullOrEmpty(username) && (a.Message.Contains(username) || (a.TriggerEvent != null && a.TriggerEvent.Details.Contains(username)))) return true;
                    
                    // Match Source or EventId
                    if (a.TriggerEvent != null && triggerEvent != null)
                    {
                        if (a.TriggerEvent.Source == source) return true;
                        if (a.TriggerEvent.EventId == eventId) return true;
                    }

                    return false;
                });

                if (correlates)
                {
                    matchedIncident = incident;
                    break;
                }
            }

            if (matchedIncident != null)
            {
                // Link to existing incident
                alert.IncidentId = matchedIncident.Id;
                matchedIncident.Alerts.Add(alert);
                RecalculateSeverity(matchedIncident);
                matchedIncident.UpdatedAt = DateTime.UtcNow;
                
                _context.Incidents.Update(matchedIncident);
            }
            else
            {
                // Create new correlated incident
                var severity = alert.Severity == "critical" ? IncidentSeverity.Critical : 
                               (alert.Severity == "high" ? IncidentSeverity.High : 
                               (alert.Severity == "medium" ? IncidentSeverity.Medium : IncidentSeverity.Low));

                var newIncident = new Incident
                {
                    Title = $"[Auto] Incident: {ruleName} on {hostname}",
                    Description = $"Automatically correlated incident for rule '{ruleName}' triggered on agent '{hostname}'. Details: {alert.Message}",
                    Status = IncidentStatus.New,
                    Severity = severity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                newIncident.Alerts.Add(alert);
                await _context.Incidents.AddAsync(newIncident);
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
