using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface ISecurityEventRepository
    {
        /// <summary>
        /// Thêm mới một sự kiện an ninh vào CSDL.
        /// </summary>
        Task AddAsync(SecurityEvent securityEvent);

        /// <summary>
        /// Lưu tất cả thay đổi tích lũy xuống cơ sở dữ liệu.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
