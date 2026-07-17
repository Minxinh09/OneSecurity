using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Realtime;

namespace OneSecurity.Server.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly IPolicyRepository _policyRepository;
        private readonly IAuditService _auditService;
        private readonly INotificationHubService _notificationHub;
        private readonly LocalAgentDbContext _dbContext;

        public PolicyService(
            IPolicyRepository policyRepository,
            IAuditService auditService,
            INotificationHubService notificationHub,
            LocalAgentDbContext dbContext)
        {
            _policyRepository = policyRepository;
            _auditService = auditService;
            _notificationHub = notificationHub;
            _dbContext = dbContext;
        }

        public async Task<AgentPolicy?> GetByIdAsync(long id)
        {
            return await _policyRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<AgentPolicy>> GetAllAsync()
        {
            return await _policyRepository.GetAllAsync();
        }

        public async Task<AgentPolicy> CreateAsync(AgentPolicy policy)
        {
            policy.Version = 1;
            await _policyRepository.AddAsync(policy);
            await _policyRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Policy Created",
                resourceType: AuditResourceType.System,
                entityId: policy.Id.ToString(),
                description: $"Agent policy {policy.Name} was created.",
                success: true
            );

            // Invalidate collector config versions
            var collectors = await _dbContext.CollectorNodes.ToListAsync();
            foreach (var coll in collectors)
            {
                coll.ConfigurationVersion++;
            }
            await _dbContext.SaveChangesAsync();

            await _notificationHub.NotifyPolicyUpdatedAsync(policy);
            return policy;
        }

        public async Task<AgentPolicy?> UpdateAsync(long id, AgentPolicy policy)
        {
            var existing = await _policyRepository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Name = policy.Name;
            existing.HeartbeatInterval = policy.HeartbeatInterval;
            existing.MetricsInterval = policy.MetricsInterval;
            existing.EnabledLogs = policy.EnabledLogs;
            existing.ResponseEnabled = policy.ResponseEnabled;
            existing.Description = policy.Description;
            existing.Version++; // Increment version

            _policyRepository.Update(existing);
            await _policyRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Policy Assigned", // Matches auditing requirements "Policy Assigned" / updated
                resourceType: AuditResourceType.System,
                entityId: existing.Id.ToString(),
                description: $"Agent policy {existing.Name} details were updated to version {existing.Version}.",
                success: true
            );

            // Invalidate collector config versions so they sync the latest policy
            var collectors = await _dbContext.CollectorNodes.ToListAsync();
            foreach (var coll in collectors)
            {
                coll.ConfigurationVersion++;
            }
            await _dbContext.SaveChangesAsync();

            await _notificationHub.NotifyPolicyUpdatedAsync(existing);
            return existing;
        }
    }
}
