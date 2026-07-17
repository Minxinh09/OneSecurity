using System;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Realtime;

namespace OneSecurity.Server.Services
{
    public class AgentHeartbeatService : IAgentHeartbeatService
    {
        private readonly IAgentRepository _agentRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly INotificationHubService _notificationHubService;

        public AgentHeartbeatService(
            IAgentRepository agentRepository,
            IAssetRepository assetRepository,
            INotificationHubService notificationHubService)
        {
            _agentRepository = agentRepository;
            _assetRepository = assetRepository;
            _notificationHubService = notificationHubService;
        }

        public async Task<HeartbeatResponse?> ProcessHeartbeatAsync(HeartbeatRequest request)
        {
            // 1. Lấy thông tin Agent kiểm tra sự tồn tại và lấy status trước khi cập nhật
            var agentBefore = await _agentRepository.GetByIdAsync(request.AgentId);
            if (agentBefore == null)
            {
                return null;
            }

            var oldStatus = agentBefore.Status;

            // 2. Cập nhật trực tiếp xuống CSDL bằng UpdateHeartbeatAsync() sử dụng ExecuteUpdateAsync
            await _agentRepository.UpdateHeartbeatAsync(request.AgentId);

            // Cập nhật giá trị trong bộ nhớ của thực thể đang được track để đồng bộ với DB
            agentBefore.Status = "online"; // Lifecycle status: online
            agentBefore.LastSeenAt = DateTime.UtcNow;

            // 3. Load lại Agent bằng GetByIdAsync() để lấy mốc LastSeenAt và Status mới nhất từ CSDL
            var agentAfter = await _agentRepository.GetByIdAsync(request.AgentId);
            if (agentAfter == null)
            {
                return null;
            }

            // Cập nhật trạng thái LastSeen của Asset tương ứng nếu có
            if (agentAfter.AssetId.HasValue)
            {
                var asset = await _assetRepository.GetByIdAsync(agentAfter.AssetId.Value);
                if (asset != null)
                {
                    asset.LastSeen = DateTime.UtcNow;
                    _assetRepository.Update(asset);
                    await _assetRepository.SaveChangesAsync();
                }
            }

            // Broadcast Realtime Notifications
            await _notificationHubService.NotifyHeartbeatUpdatedAsync(agentAfter);
            if (oldStatus != agentAfter.Status)
            {
                await _notificationHubService.NotifyAgentStatusChangedAsync(agentAfter, oldStatus, agentAfter.Status);
            }

            // 4. Trả về Response
            return new HeartbeatResponse
            {
                AgentId = agentAfter.Id,
                Status = agentAfter.Status,
                LastSeenAt = agentAfter.LastSeenAt
            };
        }
    }
}
