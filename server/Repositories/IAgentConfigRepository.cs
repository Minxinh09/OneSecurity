using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IAgentConfigRepository
    {
        /// <summary>
        /// Lấy cấu hình mặc định (default config) từ CSDL.
        /// </summary>
        Task<AgentConfig?> GetDefaultAsync();

        /// <summary>
        /// Lấy cấu hình theo Id.
        /// </summary>
        Task<AgentConfig?> GetByIdAsync(long id);

        /// <summary>
        /// Kiểm tra cấu hình có tồn tại trong hệ thống hay chưa.
        /// </summary>
        Task<bool> ExistsAsync(long id);
    }
}
