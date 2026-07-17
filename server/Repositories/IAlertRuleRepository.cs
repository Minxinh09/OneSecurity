using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IAlertRuleRepository
    {
        /// <summary>
        /// Lấy tất cả các luật cảnh báo đang kích hoạt (Task 7).
        /// </summary>
        Task<IEnumerable<AlertRule>> GetActiveRulesAsync();

        /// <summary>
        /// Kiểm tra sự tồn tại của luật cảnh báo theo ID.
        /// </summary>
        Task<bool> ExistsAsync(long id);

        /// <summary>
        /// Kiểm tra xem tên luật đã được sử dụng hay chưa (có thể bỏ qua một ID cụ thể khi chỉnh sửa).
        /// </summary>
        Task<bool> ExistsNameAsync(string name, long? excludeId);

        /// <summary>
        /// Lấy thông tin luật cảnh báo theo ID (đọc không theo dõi).
        /// </summary>
        Task<AlertRule?> GetByIdAsync(long id);

        /// <summary>
        /// Lấy danh sách luật cảnh báo phân trang và lọc (đọc không theo dõi).
        /// </summary>
        Task<List<AlertRule>> GetPagedAsync(string? name, bool? isEnabled, int page, int pageSize);

        /// <summary>
        /// Đếm số lượng luật cảnh báo có lọc.
        /// </summary>
        Task<int> CountAsync(string? name, bool? isEnabled);

        /// <summary>
        /// Thêm mới luật cảnh báo (không dùng AsNoTracking).
        /// </summary>
        Task AddAsync(AlertRule rule);

        /// <summary>
        /// Cập nhật luật cảnh báo (không dùng AsNoTracking).
        /// </summary>
        void Update(AlertRule rule);

        /// <summary>
        /// Xóa luật cảnh báo (không dùng AsNoTracking).
        /// </summary>
        void Delete(AlertRule rule);

        /// <summary>
        /// Lưu tất cả thay đổi tích lũy xuống cơ sở dữ liệu.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
