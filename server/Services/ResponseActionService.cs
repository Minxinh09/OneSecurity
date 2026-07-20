using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class ResponseActionService : IResponseActionService
    {
        private readonly IResponseActionRepository _repository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LocalAgentDbContext _context;

        public ResponseActionService(
            IResponseActionRepository repository,
            UserManager<ApplicationUser> userManager,
            LocalAgentDbContext context)
        {
            _repository = repository;
            _userManager = userManager;
            _context = context;
        }

        public async Task<ResponseActionDto> CreateAsync(CreateResponseActionRequest request, ClaimsPrincipal claimsPrincipal)
        {
            // Map frontend compatibility aliases to canonical database enums
            if (request.ActionType == ResponseActionType.RestartSqlServer)
            {
                request.ActionType = ResponseActionType.RestartSQL;
            }
            else if (request.ActionType == ResponseActionType.SyncConfig)
            {
                request.ActionType = ResponseActionType.SyncConfiguration;
            }
            else if (request.ActionType == ResponseActionType.Reboot)
            {
                request.ActionType = ResponseActionType.Restart;
            }

            // 1. Verify user exists
            var userId = _userManager.GetUserId(claimsPrincipal);
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            var isOperator = await _userManager.IsInRoleAsync(user, "Operator");
            var isSecOp = await _userManager.IsInRoleAsync(user, "SecurityOperator");

            // Viewer or unprivileged roles are blocked with 403 Forbidden
            if (!isAdmin && !isOperator && !isSecOp)
            {
                throw new UnauthorizedAccessException("You do not have permission to create response actions.");
            }

            // 2. Query Agent based on role privileges
            Agent? agent;
            if (isAdmin)
            {
                // Admin bypasses Global Query Filters
                agent = await _context.Agents
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a => a.Id == request.AgentId);
            }
            else
            {
                // Operator goes through GQF (automatically isolates hospitals)
                agent = await _context.Agents
                    .FirstOrDefaultAsync(a => a.Id == request.AgentId);
            }

            // If agent doesn't exist or is not visible, return 404 (KeyNotFoundException)
            if (agent == null)
            {
                throw new KeyNotFoundException($"Agent with ID {request.AgentId} not found.");
            }

            // 3. Verify Incident exists if supplied (> 0)
            if (request.IncidentId > 0)
            {
                var incidentExists = await _context.Incidents
                    .AnyAsync(i => i.Id == request.IncidentId);
                if (!incidentExists)
                {
                    throw new ArgumentException($"Incident with ID {request.IncidentId} not found.");
                }
            }

            // 4. Initialize ResponseAction (Status = Pending, RequestedAt = DateTime.UtcNow)
            var action = new ResponseAction
            {
                IncidentId = request.IncidentId,
                AgentId = request.AgentId,
                ActionType = request.ActionType,
                Status = ResponseStatus.Pending,
                RequestedByUserId = userId,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Parameters = request.Parameters ?? request.Metadata,
                RequestedAt = DateTime.UtcNow,
                HospitalId = agent.HospitalId,
                CreatedBy = user.UserName
            };

            await _repository.AddAsync(action);
            await _repository.SaveChangesAsync();

            // 5. Return ResponseActionDto with complete fields
            return new ResponseActionDto
            {
                Id = action.Id,
                IncidentId = action.IncidentId,
                AgentId = action.AgentId,
                AgentHostname = agent.Hostname,
                ActionType = action.ActionType.ToString(),
                Status = action.Status.ToString(),
                RequestedByUserId = action.RequestedByUserId,
                RequestedByUserName = user.UserName ?? string.Empty,
                RequestedAt = action.RequestedAt,
                CorrelationId = action.CorrelationId,
                Parameters = action.Parameters,
                HospitalId = action.HospitalId,
                CreatedBy = action.CreatedBy,
                Output = action.Output,
                ErrorMessage = action.ErrorMessage
            };
        }

        public async Task<CreateResponseActionResponse> CreateActionAsync(CreateResponseActionRequest request, string userId)
        {
            // Map frontend compatibility aliases to canonical database enums
            if (request.ActionType == ResponseActionType.RestartSqlServer)
            {
                request.ActionType = ResponseActionType.RestartSQL;
            }
            else if (request.ActionType == ResponseActionType.SyncConfig)
            {
                request.ActionType = ResponseActionType.SyncConfiguration;
            }
            else if (request.ActionType == ResponseActionType.Reboot)
            {
                request.ActionType = ResponseActionType.Restart;
            }

            // 1. Verify user exists
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            var isOperator = await _userManager.IsInRoleAsync(user, "Operator");
            var isSecOp = await _userManager.IsInRoleAsync(user, "SecurityOperator");

            if (!isAdmin && !isOperator && !isSecOp)
            {
                throw new UnauthorizedAccessException("You do not have permission to create response actions.");
            }

            // 2. Verify agent exists
            var agent = await _context.Agents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == request.AgentId);

            if (agent == null)
            {
                throw new ArgumentException($"Agent with ID {request.AgentId} not found.");
            }

            // 3. Verify hospital authorization
            var permittedHospitalIds = _context.PermittedHospitalIds;
            if (!isAdmin)
            {
                bool hasAccess = agent.HospitalId.HasValue && 
                                 permittedHospitalIds != null && 
                                 permittedHospitalIds.Contains(agent.HospitalId.Value);

                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException("You do not have access to this agent's hospital.");
                }
            }

            // 5. Initialize ResponseAction (Status must start as Pending)
            var action = new ResponseAction
            {
                IncidentId = request.IncidentId,
                AgentId = request.AgentId,
                ActionType = request.ActionType,
                Status = ResponseStatus.Pending, // Always start as Pending
                RequestedByUserId = userId,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Parameters = request.Parameters ?? request.Metadata,
                RequestedAt = DateTime.UtcNow, // Server timestamp (Never trust client)
                HospitalId = agent.HospitalId,
                CreatedBy = user.UserName
            };

            await _repository.AddAsync(action);
            await _repository.SaveChangesAsync();

            return new CreateResponseActionResponse
            {
                Id = action.Id,
                AgentId = action.AgentId,
                ActionType = action.ActionType.ToString(),
                Status = action.Status.ToString(),
                CorrelationId = action.CorrelationId
            };
        }

        public async Task<ResponseActionDto?> GetByIdAsync(long id, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            var permittedHospitalIds = _context.PermittedHospitalIds;

            var action = await _repository.GetByIdAsync(id);
            if (action == null) return null;

            // Check hospital access
            if (!isAdmin)
            {
                bool hasAccess = action.HospitalId.HasValue && 
                                 permittedHospitalIds != null && 
                                 permittedHospitalIds.Contains(action.HospitalId.Value);

                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException("You do not have access to this response action.");
                }
            }

            return MapToDto(action);
        }

        public async Task<List<ResponseActionDto>> GetByAgentIdAsync(string agentId, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            var permittedHospitalIds = _context.PermittedHospitalIds;

            var agent = await _context.Agents
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Id == agentId);

            if (agent == null)
            {
                throw new ArgumentException($"Agent with ID {agentId} not found.");
            }

            // Check hospital access
            if (!isAdmin)
            {
                bool hasAccess = agent.HospitalId.HasValue && 
                                 permittedHospitalIds != null && 
                                 permittedHospitalIds.Contains(agent.HospitalId.Value);

                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException("You do not have access to this agent.");
                }
            }

            var items = await _repository.GetByAgentIdAsync(agentId);
            return items.Select(MapToDto).ToList();
        }

        public async Task<List<ResponseActionDto>> GetPendingActionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            var permittedHospitalIds = _context.PermittedHospitalIds;

            var items = await _repository.GetPendingActionsAsync();

            // Filter for hospital hierarchy
            if (!isAdmin)
            {
                items = items.Where(x => x.HospitalId.HasValue && 
                                         permittedHospitalIds != null && 
                                         permittedHospitalIds.Contains(x.HospitalId.Value)).ToList();
            }

            return items.Select(MapToDto).ToList();
        }

        public async Task<ResponseActionDto?> CancelActionAsync(long id, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            var permittedHospitalIds = _context.PermittedHospitalIds;

            var action = await _repository.GetByIdAsync(id);
            if (action == null) return null;

            // Check hospital access
            if (!isAdmin)
            {
                bool hasAccess = action.HospitalId.HasValue && 
                                 permittedHospitalIds != null && 
                                 permittedHospitalIds.Contains(action.HospitalId.Value);

                if (!hasAccess)
                {
                    throw new UnauthorizedAccessException("You do not have access to this response action.");
                }
            }

            // Only Pending actions can be cancelled
            if (action.Status != ResponseStatus.Pending)
            {
                throw new InvalidOperationException("Only Pending response actions can be cancelled.");
            }

            action.Status = ResponseStatus.Cancelled;
            action.CompletedAt = DateTime.UtcNow;

            _repository.Update(action);
            await _repository.SaveChangesAsync();

            return MapToDto(action);
        }

        public async Task<ResponseActionListResponse> GetPagedAsync(
            int page, 
            int pageSize, 
            string? status, 
            string? actionType, 
            string? agentId, 
            string? requestedBy,
            string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found.");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            var permittedHospitalIds = _context.PermittedHospitalIds;

            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, status, actionType, agentId, requestedBy);

            // Filter for hospital hierarchy
            if (!isAdmin)
            {
                items = items.Where(x => x.HospitalId.HasValue && 
                                         permittedHospitalIds != null && 
                                         permittedHospitalIds.Contains(x.HospitalId.Value)).ToList();
                totalCount = items.Count; // Adjust total count to match filtered items
            }

            return new ResponseActionListResponse
            {
                TotalItems = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = items.Select(MapToDto).ToList()
            };
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
                HospitalId = action.HospitalId,
                CreatedBy = action.CreatedBy,
                Output = action.Output
            };
        }
    }
}
