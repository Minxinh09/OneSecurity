using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Realtime;

namespace OneSecurity.Server.Services
{
    public class AgentRegistrationService : IAgentRegistrationService
    {
        private readonly IAgentRepository _agentRepository;
        private readonly IAgentConfigRepository _configRepository;
        private readonly INotificationHubService _notificationHubService;
        private readonly IEnrollmentService _enrollmentService;
        private readonly IAssetRepository _assetRepository;
        private readonly LocalAgentDbContext _dbContext;

        public AgentRegistrationService(
            IAgentRepository agentRepository, 
            IAgentConfigRepository configRepository,
            INotificationHubService notificationHubService,
            IEnrollmentService enrollmentService,
            IAssetRepository assetRepository,
            LocalAgentDbContext dbContext)
        {
            _agentRepository = agentRepository;
            _configRepository = configRepository;
            _notificationHubService = notificationHubService;
            _enrollmentService = enrollmentService;
            _assetRepository = assetRepository;
            _dbContext = dbContext;
        }

        public async Task<RegisterAgentResponse?> RegisterAsync(RegisterAgentRequest request)
        {
            var existingAgent = await _agentRepository.GetByHostnameAsync(request.Hostname);
            
            if (existingAgent != null)
            {
                var oldStatus = existingAgent.Status;
                existingAgent.IpAddress = request.IpAddress;
                existingAgent.OsInfo = request.OsInfo;
                existingAgent.SupportedActions = request.SupportedActions;
                existingAgent.Capabilities = request.Capabilities;
                existingAgent.AgentVersion = request.AgentVersion;
                existingAgent.CollectorVersion = request.CollectorVersion;
                existingAgent.Status = "registered";
                existingAgent.LastSeenAt = DateTime.UtcNow;

                await _agentRepository.SaveChangesAsync();

                if (existingAgent.AssetId.HasValue)
                {
                    var asset = await _assetRepository.GetByIdAsync(existingAgent.AssetId.Value);
                    if (asset != null)
                    {
                        asset.Status = "Managed";
                        asset.LastSeen = DateTime.UtcNow;
                        _assetRepository.Update(asset);
                        await _assetRepository.SaveChangesAsync();
                    }
                }

                await _notificationHubService.NotifyAgentStatusChangedAsync(existingAgent, oldStatus, existingAgent.Status);

                var existingConfig = await _configRepository.GetByIdAsync(existingAgent.ConfigId);
                return new RegisterAgentResponse
                {
                    AgentId = existingAgent.Id,
                    Hostname = existingAgent.Hostname,
                    Status = existingAgent.Status,
                    HeartbeatIntervalSeconds = existingConfig?.HeartbeatIntervalSeconds ?? 10,
                    RegisteredAt = existingAgent.RegisteredAt
                };
            }

            if (string.IsNullOrEmpty(request.EnrollmentToken))
            {
                var devConfig = await _configRepository.GetDefaultAsync();
                if (devConfig == null)
                    throw new InvalidOperationException("Default Agent Configuration not found in database.");

                int? parsedHospitalId = null;
                if (!string.IsNullOrEmpty(request.HospitalCode))
                {
                    _dbContext.FilterOverride = null;
                    var hosp = await _dbContext.Hospitals.FirstOrDefaultAsync(h => h.Code == request.HospitalCode);
                    if (hosp != null)
                    {
                        parsedHospitalId = hosp.Id;
                    }
                }

                var matchingAsset = await _dbContext.InfrastructureAssets.FirstOrDefaultAsync(a => a.Hostname.ToLower() == request.Hostname.ToLower());

                var devAgentId = Guid.NewGuid().ToString();
                var devAgent = new Agent
                {
                    Id = devAgentId,
                    Hostname = request.Hostname,
                    IpAddress = request.IpAddress,
                    OsInfo = request.OsInfo,
                    Status = "registered",
                    ConfigId = devConfig.Id,
                    AssetId = matchingAsset?.Id,
                    HospitalId = matchingAsset?.HospitalId ?? parsedHospitalId,
                    RegisteredAt = DateTime.UtcNow,
                    LastSeenAt = DateTime.UtcNow,
                    SupportedActions = request.SupportedActions,
                    Capabilities = request.Capabilities,
                    AgentVersion = request.AgentVersion,
                    CollectorVersion = request.CollectorVersion
                };

                await _agentRepository.AddAsync(devAgent);
                await _agentRepository.SaveChangesAsync();

                var collector = await _dbContext.CollectorNodes.FindAsync(request.CollectorId);
                if (collector != null)
                {
                    collector.ConfigurationVersion++;
                    await _dbContext.SaveChangesAsync();
                }

                await _notificationHubService.NotifyAgentStatusChangedAsync(devAgent, "None", devAgent.Status);

                return new RegisterAgentResponse
                {
                    AgentId = devAgent.Id,
                    Hostname = devAgent.Hostname,
                    Status = devAgent.Status,
                    HeartbeatIntervalSeconds = devConfig.HeartbeatIntervalSeconds,
                    RegisteredAt = devAgent.RegisteredAt
                };
            }

            var token = await _enrollmentService.ValidateAndConsumeTokenAsync(request.EnrollmentToken, request.Hostname, request.CollectorId);
            if (token == null)
            {
                throw new ArgumentException("Invalid, expired, or mismatched enrollment token.");
            }

            var assetToUpdate = await _assetRepository.GetByIdAsync(token.AssetId);

            var defaultConfig = await _configRepository.GetDefaultAsync();
            if (defaultConfig == null)
            {
                throw new InvalidOperationException("Default Agent Configuration not found in database.");
            }

            var newAgentId = Guid.NewGuid().ToString();

            var agent = new Agent
            {
                Id = newAgentId,
                Hostname = request.Hostname,
                IpAddress = request.IpAddress,
                OsInfo = request.OsInfo,
                Status = "registered",
                ConfigId = defaultConfig.Id, 
                AssetId = token.AssetId,
                HospitalId = assetToUpdate?.HospitalId,
                RegisteredAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                SupportedActions = request.SupportedActions,
                Capabilities = request.Capabilities,
                AgentVersion = request.AgentVersion,
                CollectorVersion = request.CollectorVersion
            };

            await _agentRepository.AddAsync(agent);
            var affectedRows = await _agentRepository.SaveChangesAsync();
            if (affectedRows <= 0)
            {
                throw new InvalidOperationException("Failed to save agent registration to the database.");
            }

            var collectorNode = await _dbContext.CollectorNodes.FindAsync(request.CollectorId);
            if (collectorNode != null)
            {
                collectorNode.ConfigurationVersion++;
                await _dbContext.SaveChangesAsync();
            }

            if (assetToUpdate != null)
            {
                assetToUpdate.Status = "Managed";
                assetToUpdate.LastSeen = DateTime.UtcNow;
                _assetRepository.Update(assetToUpdate);
                await _assetRepository.SaveChangesAsync();
            }

            await _notificationHubService.NotifyAgentStatusChangedAsync(agent, "None", agent.Status);

            return new RegisterAgentResponse
            {
                AgentId = agent.Id,
                Hostname = agent.Hostname,
                Status = agent.Status,
                HeartbeatIntervalSeconds = defaultConfig.HeartbeatIntervalSeconds,
                RegisteredAt = agent.RegisteredAt
            };
        }
    }
}