using System;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Realtime;

namespace OneSecurity.Server.Services
{
    public class SecurityEventService : ISecurityEventService
    {
        private readonly IAgentRepository _agentRepository;
        private readonly ISecurityEventRepository _eventRepository;
        private readonly IAlertRepository _alertRepository;
        private readonly IRuleEngineService _ruleEngineService;
        private readonly INotificationService _notificationService;
        private readonly INotificationHubService _notificationHubService;
        private readonly IIncidentService _incidentService;
        private readonly IAssetRepository _assetRepository;

        public SecurityEventService(
            IAgentRepository agentRepository, 
            ISecurityEventRepository eventRepository,
            IAlertRepository alertRepository,
            IRuleEngineService ruleEngineService,
            INotificationService notificationService,
            INotificationHubService notificationHubService,
            IIncidentService incidentService,
            IAssetRepository assetRepository)
        {
            _agentRepository = agentRepository;
            _eventRepository = eventRepository;
            _alertRepository = alertRepository;
            _ruleEngineService = ruleEngineService;
            _notificationService = notificationService;
            _notificationHubService = notificationHubService;
            _incidentService = incidentService;
            _assetRepository = assetRepository;
        }

        public async Task<SecurityEventResponse?> IngestEventAsync(SecurityEventRequest request)
        {
            // 1. Lấy thông tin Agent kiểm tra sự tồn tại
            var agent = await _agentRepository.GetByIdAsync(request.AgentId);
            if (agent == null)
            {
                return null; // Trả về null biểu thị Agent không tồn tại (Controller trả 404)
            }

            // 2. Ánh xạ DTO sang Entity SecurityEvent
            var securityEvent = new SecurityEvent
            {
                EventId = request.EventId,
                AgentId = request.AgentId,
                Agent = agent, // Thiết lập navigation Agent
                Timestamp = DateTime.UtcNow,
                Category = request.Category,
                Severity = request.Severity,
                Source = request.Source,
                Title = request.Title,
                Details = request.Details,
                RawData = request.RawData,
                ReceivedAt = DateTime.UtcNow
            };

            // 3. Thêm mới bản ghi Event vào repository (chưa commit)
            await _eventRepository.AddAsync(securityEvent);

            // 4. Đánh giá sự kiện qua Rule Engine để sinh danh sách Alert (nếu có)
            var generatedAlerts = new List<Alert>();
            bool isMaintenance = false;

            if (agent.AssetId.HasValue)
            {
                var asset = await _assetRepository.GetByIdAsync(agent.AssetId.Value);
                if (asset != null && asset.Status == "Maintenance")
                {
                    isMaintenance = true;
                }
            }

            if (!isMaintenance)
            {
                generatedAlerts = await _ruleEngineService.EvaluateEventAsync(securityEvent);
            }

            // 5. Thêm danh sách Alert vào repository (chưa commit)
            if (generatedAlerts != null && generatedAlerts.Count > 0)
            {
                foreach (var alert in generatedAlerts)
                {
                    await _alertRepository.AddAsync(alert);
                }
            }

            // 6. Thực thi một COMMIT duy nhất (Unit of Work) cho cả Event và danh sách Alert
            var affectedRows = await _eventRepository.SaveChangesAsync();
            if (affectedRows <= 0)
            {
                throw new InvalidOperationException("Failed to save security event and generated alerts to the database.");
            }

            // --- TỰ ĐỘNG GOM NHÓM INCIDENT ---
            if (generatedAlerts != null && generatedAlerts.Count > 0)
            {
                foreach (var alert in generatedAlerts)
                {
                    try
                    {
                        await _incidentService.CorrelateAlertAsync(alert);
                    }
                    catch
                    {
                        // Tránh làm gián đoạn pipeline chính khi gom nhóm gặp lỗi
                    }
                }
            }

            // Broadcast Realtime Notifications
            await _notificationHubService.NotifySecurityEventCreatedAsync(securityEvent);
            if (generatedAlerts != null && generatedAlerts.Count > 0)
            {
                foreach (var alert in generatedAlerts)
                {
                    await _notificationHubService.NotifyAlertCreatedAsync(alert);
                }
            }

            // 7. Gửi thông báo Telegram bất đồng bộ cho các Alert vừa được lưu thành công
            if (generatedAlerts != null && generatedAlerts.Count > 0)
            {
                foreach (var alert in generatedAlerts)
                {
                    // Lệnh chạy async tự bắt lỗi bên trong và không chặn tiến trình
                    await _notificationService.SendAlertAsync(alert);
                }
            }

            // 8. Trả về SecurityEventResponse
            return new SecurityEventResponse
            {
                Id = securityEvent.Id,
                EventId = securityEvent.EventId,
                AgentId = securityEvent.AgentId,
                ReceivedAt = securityEvent.ReceivedAt
            };
        }
    }
}
