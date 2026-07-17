using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface IRuleEngineService
    {
        /// <summary>
        /// Đánh giá sự kiện an ninh thô với các luật đang kích hoạt và trả về danh sách các cảnh báo (Alert) cần được tạo.
        /// </summary>
        Task<List<Alert>> EvaluateEventAsync(SecurityEvent securityEvent);
    }
}
