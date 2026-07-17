using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;
using OneSecurity.Server.Models.Enums;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Realtime;

namespace OneSecurity.Server.Services
{
    public class AssetService : IAssetService
    {
        private readonly IAssetRepository _assetRepository;
        private readonly IAuditService _auditService;
        private readonly INotificationHubService _notificationHub;

        public AssetService(
            IAssetRepository assetRepository,
            IAuditService auditService,
            INotificationHubService notificationHub)
        {
            _assetRepository = assetRepository;
            _auditService = auditService;
            _notificationHub = notificationHub;
        }

        public async Task<InfrastructureAsset?> GetByIdAsync(long id)
        {
            return await _assetRepository.GetByIdAsync(id);
        }

        public async Task<InfrastructureAsset?> GetByHostnameAsync(string hostname)
        {
            return await _assetRepository.GetByHostnameAsync(hostname);
        }

        public async Task<IEnumerable<InfrastructureAsset>> GetAllAsync()
        {
            return await _assetRepository.GetAllAsync();
        }

        public async Task<InfrastructureAsset> CreateAsync(InfrastructureAsset asset)
        {
            asset.CreatedAt = DateTime.UtcNow;
            asset.UpdatedAt = DateTime.UtcNow;
            asset.Status = "PendingEnrollment"; // Default status

            await _assetRepository.AddAsync(asset);
            await _assetRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Asset Created",
                resourceType: AuditResourceType.System, // map to System
                entityId: asset.Id.ToString(),
                description: $"Asset {asset.Hostname} was created under department {asset.Department}.",
                success: true
            );

            await _notificationHub.NotifyAssetCreatedAsync(asset);
            return asset;
        }

        public async Task<InfrastructureAsset?> UpdateAsync(long id, InfrastructureAsset asset)
        {
            var existing = await _assetRepository.GetByIdAsync(id);
            if (existing == null) return null;

            existing.Hostname = asset.Hostname;
            existing.IPAddress = asset.IPAddress;
            existing.OperatingSystem = asset.OperatingSystem;
            existing.OperatingSystemVersion = asset.OperatingSystemVersion;
            existing.Domain = asset.Domain;
            existing.Department = asset.Department;
            existing.Building = asset.Building;
            existing.Owner = asset.Owner;
            existing.Description = asset.Description;
            existing.Criticality = asset.Criticality;
            existing.AssetType = asset.AssetType;
            existing.CollectorId = asset.CollectorId;
            existing.PolicyId = asset.PolicyId;
            existing.UpdatedAt = DateTime.UtcNow;

            _assetRepository.Update(existing);
            await _assetRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Asset Updated",
                resourceType: AuditResourceType.System,
                entityId: existing.Id.ToString(),
                description: $"Asset {existing.Hostname} details were updated.",
                success: true
            );

            await _notificationHub.NotifyAssetUpdatedAsync(existing);
            return existing;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var existing = await _assetRepository.GetByIdAsync(id);
            if (existing == null) return false;

            _assetRepository.Delete(existing);
            await _assetRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Asset Deleted",
                resourceType: AuditResourceType.System,
                entityId: id.ToString(),
                description: $"Asset {existing.Hostname} was deleted.",
                success: true
            );

            return true;
        }

        public async Task<bool> TransitionStatusAsync(long id, string targetStatus)
        {
            var asset = await _assetRepository.GetByIdAsync(id);
            if (asset == null) return false;

            if (!IsValidTransition(asset.Status, targetStatus))
            {
                return false;
            }

            string oldStatus = asset.Status;
            asset.Status = targetStatus;
            asset.UpdatedAt = DateTime.UtcNow;

            _assetRepository.Update(asset);
            await _assetRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                action: "Asset Status Transitioned",
                resourceType: AuditResourceType.System,
                entityId: asset.Id.ToString(),
                description: $"Asset {asset.Hostname} transitioned status from {oldStatus} to {targetStatus}.",
                success: true
            );

            await _notificationHub.NotifyAssetUpdatedAsync(asset);
            return true;
        }

        private bool IsValidTransition(string from, string to)
        {
            if (from == to) return true;
            return from switch
            {
                "Discovered" => to == "PendingEnrollment" || to == "Retired",
                "PendingEnrollment" => to == "Managed" || to == "Retired",
                "Managed" => to == "Maintenance" || to == "Retired",
                "Maintenance" => to == "Managed" || to == "Retired",
                "Retired" => to == "Discovered",
                _ => false
            };
        }
    }
}
