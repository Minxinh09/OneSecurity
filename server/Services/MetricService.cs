using System;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Realtime;

namespace OneSecurity.Server.Services
{
    public class MetricService : IMetricService
    {
        private readonly IAgentRepository _agentRepository;
        private readonly IMetricRepository _metricRepository;
        private readonly INotificationHubService _notificationHubService;

        public MetricService(
            IAgentRepository agentRepository,
            IMetricRepository metricRepository,
            INotificationHubService notificationHubService)
        {
            _agentRepository = agentRepository;
            _metricRepository = metricRepository;
            _notificationHubService = notificationHubService;
        }

        public async Task<MetricResponse?> IngestMetricAsync(MetricRequest request)
        {
            // 1. Lấy thông tin Agent kiểm tra sự tồn tại
            var agent = await _agentRepository.GetByIdAsync(request.AgentId);
            if (agent == null)
            {
                return null; // Trả về null biểu thị Agent không tồn tại (Controller trả 404)
            }

            // 2. Ánh xạ DTO sang Entity MetricRecord và gán Timestamp = DateTime.UtcNow
            var record = new MetricRecord
            {
                AgentId = request.AgentId,
                Agent = agent, // Thiết lập navigation Agent
                Timestamp = DateTime.UtcNow,
                CpuUsagePercent = request.CpuUsagePercent,
                RamUsagePercent = request.RamUsagePercent,
                DiskUsagePercent = request.DiskUsagePercent,
                NetworkInBytes = request.NetworkInBytes,
                NetworkOutBytes = request.NetworkOutBytes
            };

            // 3. Thêm mới bản ghi vào repository
            await _metricRepository.AddAsync(record);

            // 4. Lưu thay đổi và kiểm tra kết quả SaveChangesAsync() > 0
            var affectedRows = await _metricRepository.SaveChangesAsync();
            if (affectedRows <= 0)
            {
                throw new InvalidOperationException("Failed to save metric record to the database.");
            }

            // Broadcast Realtime Notification
            await _notificationHubService.NotifyMetricUpdatedAsync(record);

            // 5. Trả về MetricResponse
            return new MetricResponse
            {
                MetricRecordId = record.Id,
                AgentId = record.AgentId,
                Timestamp = record.Timestamp
            };
        }
    }
}
