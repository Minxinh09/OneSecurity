using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Services
{
    public class AlertManagementService : IAlertManagementService
    {
        private readonly IAlertRepository _alertRepository;

        public AlertManagementService(IAlertRepository alertRepository)
        {
            _alertRepository = alertRepository;
        }

        public async Task<AlertListResponse> GetPagedAsync(AlertFilterRequest filter)
        {
            // 1. Đếm tổng số bản ghi thỏa mãn bộ lọc
            var totalItems = await _alertRepository.CountAsync(
                filter.Severity,
                filter.Category,
                filter.AgentId,
                filter.IsAcknowledged);

            // 2. Lấy danh sách bản ghi phân trang và lọc
            var items = await _alertRepository.GetPagedAsync(
                filter.Severity,
                filter.Category,
                filter.AgentId,
                filter.IsAcknowledged,
                filter.Page,
                filter.PageSize);

            // 3. Ánh xạ sang AlertListItemDto
            var dtos = items.Select(a => new AlertListItemDto
            {
                Id = a.Id,
                RuleName = a.RuleName,
                Severity = a.Severity,
                Category = a.Category,
                Title = a.Title,
                AgentHostname = a.Agent?.Hostname ?? "Unknown Agent",
                CreatedAt = a.CreatedAt,
                IsAcknowledged = a.IsAcknowledged
            }).ToList();

            return new AlertListResponse
            {
                TotalItems = totalItems,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = dtos
            };
        }

        public async Task<AlertDetailDto?> GetDetailAsync(long id)
        {
            var alert = await _alertRepository.GetByIdAsync(id);
            if (alert == null)
            {
                return null;
            }

            return new AlertDetailDto
            {
                Id = alert.Id,
                RuleName = alert.RuleName,
                Severity = alert.Severity,
                Category = alert.Category,
                Title = alert.Title,
                Message = alert.Message,
                AgentHostname = alert.Agent?.Hostname ?? "Unknown Agent",
                CreatedAt = alert.CreatedAt,
                IsAcknowledged = alert.IsAcknowledged,
                TelegramSent = alert.TelegramSent
            };
        }

        public async Task<bool> AcknowledgeAsync(long id, string username)
        {
            // 1. Kiểm tra tồn tại
            var exists = await _alertRepository.ExistsAsync(id);
            if (!exists)
            {
                return false;
            }

            // 2. Lấy bản ghi để cập nhật (để lưu changes, ta lấy tracked từ DB hoặc update trực tiếp)
            // Lấy từ repository (nhận thực thể theo dõi để sửa)
            var alert = await _alertRepository.GetByIdAsync(id);
            if (alert == null)
            {
                return false;
            }

            // 3. Nghiệp vụ: Cập nhật thông tin xác nhận
            alert.IsAcknowledged = true;
            alert.AcknowledgedAt = DateTime.UtcNow;
            alert.AcknowledgedBy = username;

            // 4. Gọi Repository để cập nhật
            _alertRepository.Update(alert);

            // 5. Lưu thay đổi
            await _alertRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BulkAcknowledgeAsync(List<long> ids, string username)
        {
            if (ids == null || ids.Count == 0)
            {
                return false;
            }

            // 1. Lấy danh sách các cảnh báo theo danh sách ID
            var alerts = await _alertRepository.GetByIdsAsync(ids);
            if (alerts.Count == 0)
            {
                return false;
            }

            // 2. Nghiệp vụ: Cập nhật thông tin xác nhận cho từng Alert chưa được xử lý
            foreach (var alert in alerts)
            {
                alert.IsAcknowledged = true;
                alert.AcknowledgedAt = DateTime.UtcNow;
                alert.AcknowledgedBy = username;
            }

            // 3. Gọi Repository để cập nhật
            _alertRepository.UpdateRange(alerts);

            // 4. Lưu thay đổi
            await _alertRepository.SaveChangesAsync();
            return true;
        }
    }
}
