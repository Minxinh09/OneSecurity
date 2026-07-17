using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IAlertRepository
    {
        /// <summary>
        /// Thêm mới một cảnh báo (Alert) vào CSDL (Task 7).
        /// </summary>
        Task AddAsync(Alert alert);

        /// <summary>
        /// Kiểm tra sự tồn tại của cảnh báo theo ID.
        /// </summary>
        Task<bool> ExistsAsync(long id);

        /// <summary>
        /// Lấy thông tin cảnh báo theo ID (đọc không theo dõi, Include Agent).
        /// </summary>
        Task<Alert?> GetByIdAsync(long id);

        /// <summary>
        /// Lấy danh sách cảnh báo theo danh sách ID (đọc không theo dõi).
        /// </summary>
        Task<List<Alert>> GetByIdsAsync(IEnumerable<long> ids);

        /// <summary>
        /// Lấy danh sách cảnh báo phân trang có lọc (đọc không theo dõi, Include Agent).
        /// </summary>
        Task<List<Alert>> GetPagedAsync(
            string? severity,
            string? category,
            string? agentId,
            bool? isAcknowledged,
            int page,
            int pageSize);

        /// <summary>
        /// Đếm số lượng cảnh báo có lọc.
        /// </summary>
        Task<int> CountAsync(
            string? severity,
            string? category,
            string? agentId,
            bool? isAcknowledged);

        /// <summary>
        /// Đánh dấu cập nhật một cảnh báo (không dùng AsNoTracking).
        /// </summary>
        void Update(Alert alert);

        /// <summary>
        /// Đánh dấu cập nhật nhiều cảnh báo (không dùng AsNoTracking).
        /// </summary>
        void UpdateRange(IEnumerable<Alert> alerts);

        /// <summary>
        /// Lưu tất cả thay đổi tích lũy xuống cơ sở dữ liệu.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
