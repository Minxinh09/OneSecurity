using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface ICommandQueueService
    {
        /// <summary>
        /// Lấy lệnh Pending tiếp theo (FIFO) cho một Agent cụ thể mà không cập nhật trạng thái.
        /// </summary>
        Task<ResponseAction?> GetNextCommandAsync(string agentId);
    }
}