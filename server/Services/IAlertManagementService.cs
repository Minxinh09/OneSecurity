using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IAlertManagementService
    {
        /// <summary>
        /// Lấy danh sách cảnh báo phân trang và lọc theo yêu cầu.
        /// </summary>
        Task<AlertListResponse> GetPagedAsync(AlertFilterRequest filter);

        /// <summary>
        /// Lấy thông tin chi tiết cảnh báo theo ID.
        /// </summary>
        Task<AlertDetailDto?> GetDetailAsync(long id);

        /// <summary>
        /// Xác nhận đã xử lý một cảnh báo.
        /// </summary>
        Task<bool> AcknowledgeAsync(long id, string username);

        /// <summary>
        /// Xác nhận đã xử lý danh sách nhiều cảnh báo cùng lúc.
        /// </summary>
        Task<bool> BulkAcknowledgeAsync(List<long> ids, string username);
    }
}
