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
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IEnrollmentRepository _enrollmentRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly ICollectorRepository _collectorRepository;
        private readonly IAuditService _auditService;
        private readonly INotificationHubService _notificationHub;
        private readonly LocalAgentDbContext _dbContext;

        public EnrollmentService(
            IEnrollmentRepository enrollmentRepository,
            IAssetRepository assetRepository,
            IPolicyRepository policyRepository,
            ICollectorRepository collectorRepository,
            IAuditService auditService,
            INotificationHubService notificationHub,
            LocalAgentDbContext dbContext)
        {
            _enrollmentRepository = enrollmentRepository;
            _assetRepository = assetRepository;
            _policyRepository = policyRepository;
            _collectorRepository = collectorRepository;
            _auditService = auditService;
            _notificationHub = notificationHub;
            _dbContext = dbContext;
        }

        public async Task<EnrollmentToken> GenerateTokenAsync(long assetId, long policyId, long collectorId, int maxUses, string? reason, string? createdBy)
        {
            var asset = await _assetRepository.GetByIdAsync(assetId);
            if (asset == null) throw new ArgumentException("Asset not found");

            var policy = await _policyRepository.GetByIdAsync(policyId);
            if (policy == null) throw new ArgumentException("Policy not found");

            var collector = await _collectorRepository.GetByIdAsync(collectorId);
            if (collector == null) throw new ArgumentException("Collector not found");

            var token = new EnrollmentToken
            {
                Token = Guid.NewGuid().ToString("N"),
                AssetId = assetId,
                PolicyId = policyId,
                CollectorId = collectorId,
                ExpireAt = DateTime.UtcNow.AddDays(1), // Default 24h expiration
                Used = false,
                UsedCount = 0,
                MaxUses = maxUses > 0 ? maxUses : 1,
                Reason = reason ?? "Standard registration token",
                CreatedBy = createdBy ?? "System",
                CreatedAt = DateTime.UtcNow
            };

            await _enrollmentRepository.AddAsync(token);
            await _enrollmentRepository.SaveChangesAsync();

            // Invalidate collector config so it syncs this new token
            collector.ConfigurationVersion++;
            _collectorRepository.Update(collector);
            await _collectorRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Enrollment Generated",
                resourceType: AuditResourceType.System,
                entityId: token.Id.ToString(),
                description: $"Token generated for asset {asset.Hostname} and collector {collector.Name}.",
                success: true
            );

            await _notificationHub.NotifyEnrollmentGeneratedAsync(token);
            return token;
        }

        public async Task<EnrollmentToken?> ValidateAndConsumeTokenAsync(string tokenString, string hostname, long collectorId)
        {
            var token = await _enrollmentRepository.GetByTokenAsync(tokenString);
            if (token == null) return null;

            if (token.Used || token.UsedCount >= token.MaxUses)
            {
                return null;
            }

            if (DateTime.UtcNow > token.ExpireAt)
            {
                return null;
            }

            if (token.CollectorId != collectorId)
            {
                return null;
            }

            if (token.Asset == null || token.Asset.Hostname.ToLower() != hostname.ToLower())
            {
                return null;
            }

            // Consume token
            token.UsedCount++;
            token.UsedAt = DateTime.UtcNow;
            if (token.UsedCount >= token.MaxUses)
            {
                token.Used = true;
            }

            _enrollmentRepository.Update(token);
            await _enrollmentRepository.SaveChangesAsync();

            // Invalidate collector config versions
            var collector = await _collectorRepository.GetByIdAsync(collectorId);
            if (collector != null)
            {
                collector.ConfigurationVersion++;
                _collectorRepository.Update(collector);
                await _collectorRepository.SaveChangesAsync();
            }

            await _auditService.LogAsync(
                action: "Enrollment Consumed",
                resourceType: AuditResourceType.System,
                entityId: token.Id.ToString(),
                description: $"Token successfully consumed by agent on hostname {hostname}.",
                success: true
            );

            return token;
        }

        public async Task<IEnumerable<EnrollmentToken>> GetAllTokensAsync()
        {
            return await _enrollmentRepository.GetAllAsync();
        }

        public async Task<EnrollmentToken?> GetTokenDetailsAsync(long id)
        {
            return await _enrollmentRepository.GetByIdAsync(id);
        }
    }
}
