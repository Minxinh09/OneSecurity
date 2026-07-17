using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IAgentRepository
    {
        /// <summary>
        /// Lấy thông tin máy trạm qua khóa chính Id (string UUID) của database.
        /// </summary>
        Task<Agent?> GetByIdAsync(string id);

        /// <summary>
        /// Tìm kiếm máy trạm qua tên Hostname.
        /// </summary>
        Task<Agent?> GetByHostnameAsync(string hostname);

        /// <summary>
        /// Kiểm tra xem máy trạm có tồn tại trong hệ thống hay chưa dựa trên Id.
        /// </summary>
        Task<bool> ExistsAsync(string id);

        /// <summary>
        /// Thêm mới một máy trạm vào cơ sở dữ liệu (Đăng ký mới).
        /// </summary>
        Task AddAsync(Agent agent);

        /// <summary>
        /// Đánh dấu cập nhật thông tin chung của máy trạm.
        /// </summary>
        void Update(Agent agent);

        /// <summary>
        /// Cập nhật nhanh tín hiệu Heartbeat (Status sang "online" và cập nhật LastSeenAt).
        /// </summary>
        Task UpdateHeartbeatAsync(string id);

        /// <summary>
        /// Cập nhật nhanh trạng thái hoạt động (online, offline, warning) của máy trạm.
        /// </summary>
        Task UpdateStatusAsync(string id, string status);

        /// <summary>
        /// Lấy danh sách các máy trạm đã lâu không gửi heartbeat (quá mốc cutoffTime) để quét offline.
        /// </summary>
        Task<IEnumerable<Agent>> GetOfflineAgentsAsync(DateTime cutoffTime);

        /// <summary>
        /// Lưu tất cả thay đổi tích lũy xuống cơ sở dữ liệu. Trả về số dòng bị ảnh hưởng.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
