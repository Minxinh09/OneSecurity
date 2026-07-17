using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Realtime;

namespace OneSecurity.Server.Services
{
    public class CollectorService : ICollectorService
    {
        private readonly ICollectorRepository _collectorRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAuditService _auditService;
        private readonly INotificationHubService _notificationHub;
        private readonly LocalAgentDbContext _dbContext; // For bulk retrieval on sync

        public CollectorService(
            ICollectorRepository collectorRepository,
            IAssetRepository assetRepository,
            IPolicyRepository policyRepository,
            IEnrollmentRepository enrollmentRepository,
            IAuditService auditService,
            INotificationHubService notificationHub,
            LocalAgentDbContext dbContext)
        {
            _collectorRepository = collectorRepository;
            _assetRepository = assetRepository;
            _policyRepository = policyRepository;
            _enrollmentRepository = enrollmentRepository;
            _auditService = auditService;
            _notificationHub = notificationHub;
            _dbContext = dbContext;
        }

        public async Task<CollectorNode?> GetByIdAsync(long id)
        {
            return await _collectorRepository.GetByIdAsync(id);
        }

        public async Task<CollectorNode?> GetByKeyAsync(string key)
        {
            return await _collectorRepository.GetByKeyAsync(key);
        }

        public async Task<IEnumerable<CollectorNode>> GetAllAsync()
        {
            return await _collectorRepository.GetAllAsync();
        }

        public async Task<CollectorNode> CreateAsync(CollectorNode collector)
        {
            collector.SharedSecret = HashSecret(collector.SharedSecret);
            collector.Status = "Offline";
            collector.LastHeartbeat = DateTime.UtcNow;
            collector.LastSync = DateTime.UtcNow;
            collector.ConfigurationVersion = 1;
            collector.RulesVersion = 1;

            await _collectorRepository.AddAsync(collector);
            await _collectorRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Collector Registered",
                resourceType: AuditResourceType.System,
                entityId: collector.Id.ToString(),
                description: $"Collector {collector.Name} was registered with key {collector.CollectorKey}.",
                success: true
            );

            await _notificationHub.NotifyCollectorStatusChangedAsync(collector);
            return collector;
        }

        public async Task<CollectorNode?> UpdateAsync(long id, CollectorNode collector)
        {
            var existing = await _collectorRepository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Name = collector.Name;
            existing.IPAddress = collector.IPAddress;
            existing.Location = collector.Location;
            existing.Description = collector.Description;
            existing.Version = collector.Version;
            
            if (!string.IsNullOrWhiteSpace(collector.SharedSecret) && collector.SharedSecret != existing.SharedSecret)
            {
                existing.SharedSecret = HashSecret(collector.SharedSecret);
            }

            existing.ConfigurationVersion++; // Increment config version on update
            
            _collectorRepository.Update(existing);
            await _collectorRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Collector Configuration Updated",
                resourceType: AuditResourceType.System,
                entityId: existing.Id.ToString(),
                description: $"Collector {existing.Name} configuration was updated to version {existing.ConfigurationVersion}.",
                success: true
            );

            await _notificationHub.NotifyCollectorStatusChangedAsync(existing);
            return existing;
        }

        public async Task<CollectorSyncData?> SyncCollectorAsync(long id, string secret, int configVersion, int rulesVersion)
        {
            var collector = await _collectorRepository.GetByIdAsync(id);
            if (collector == null)
            {
                await _auditService.LogAsync(
                    action: "Collector Sync Failed",
                    resourceType: AuditResourceType.System,
                    entityId: id.ToString(),
                    description: $"Collector sync request failed: Collector ID {id} not found.",
                    success: false
                );
                return null;
            }

            var hashedSecret = HashSecret(secret);
            if (collector.SharedSecret != hashedSecret)
            {
                await _auditService.LogAsync(
                    action: "Collector Sync Failed",
                    resourceType: AuditResourceType.System,
                    entityId: id.ToString(),
                    description: $"Collector sync request failed: Invalid secret for Collector {collector.Name}.",
                    success: false
                );
                return null;
            }

            // Sync successfully authenticated
            string oldStatus = collector.Status;
            collector.Status = "Online";
            collector.LastSync = DateTime.UtcNow;
            collector.LastHeartbeat = DateTime.UtcNow;

            _collectorRepository.Update(collector);
            await _collectorRepository.SaveChangesAsync();

            if (oldStatus != "Online")
            {
                await _notificationHub.NotifyCollectorStatusChangedAsync(collector);
            }

            // Version check optimization
            if (configVersion == collector.ConfigurationVersion && rulesVersion == collector.RulesVersion)
            {
                return new CollectorSyncData
                {
                    IsUpToDate = true,
                    ConfigurationVersion = collector.ConfigurationVersion,
                    RulesVersion = collector.RulesVersion
                };
            }

            // Load updated cache payloads
            var assets = await _dbContext.InfrastructureAssets
                .Include(a => a.Agents)
                .Where(a => a.CollectorId == id)
                .ToListAsync();

            var policies = await _dbContext.AgentPolicies.ToListAsync();

            var tokens = await _dbContext.EnrollmentTokens
                .Include(t => t.Asset)
                .Where(t => t.CollectorId == id && !t.Used && t.ExpireAt > DateTime.UtcNow)
                .ToListAsync();

            var unlinkedAgents = await _dbContext.Agents
                .Where(a => a.AssetId == null)
                .ToListAsync();

            var registeredHostnames = assets.Select(a => a.Hostname).ToList();
            foreach (var ua in unlinkedAgents)
            {
                if (!registeredHostnames.Contains(ua.Hostname))
                {
                    registeredHostnames.Add(ua.Hostname);
                }
            }

            var agentIdToHostnameMap = assets
                .SelectMany(a => a.Agents)
                .ToDictionary(a => a.Id, a => a.Hostname);
            foreach (var ua in unlinkedAgents)
            {
                agentIdToHostnameMap[ua.Id] = ua.Hostname;
            }

            var assetCollectorMap = assets
                .ToDictionary(a => a.Hostname, a => a.CollectorId);
            foreach (var ua in unlinkedAgents)
            {
                assetCollectorMap[ua.Hostname] = id;
            }

            var syncData = new CollectorSyncData
            {
                IsUpToDate = false,
                ConfigurationVersion = collector.ConfigurationVersion,
                RulesVersion = collector.RulesVersion,
                
                RegisteredAssetHostnames = registeredHostnames,
                
                AgentIdToHostnameMap = agentIdToHostnameMap,
                
                AssetCollectorMap = assetCollectorMap,

                ValidTokens = tokens.Select(t => new CachedTokenDto
                {
                    Token = t.Token,
                    AssetId = t.AssetId,
                    Hostname = t.Asset?.Hostname ?? string.Empty,
                    CollectorId = t.CollectorId,
                    PolicyId = t.PolicyId,
                    ExpireAt = t.ExpireAt,
                    Used = t.Used,
                    MaxUses = t.MaxUses,
                    UsedCount = t.UsedCount
                }).ToList(),

                Policies = policies.Select(p => new CachedPolicyDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    HeartbeatInterval = p.HeartbeatInterval,
                    MetricsInterval = p.MetricsInterval,
                    EnabledLogs = p.EnabledLogs,
                    ResponseEnabled = p.ResponseEnabled
                }).ToList()
            };

            return syncData;
        }

        private string HashSecret(string secret)
        {
            if (string.IsNullOrEmpty(secret)) return string.Empty;
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(secret);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
