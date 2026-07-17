using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Repositories
{
    public interface IMetricRepository
    {
        /// <summary>
        /// Thêm mới một bản ghi hiệu năng vào CSDL.
        /// </summary>
        Task AddAsync(MetricRecord record);

        /// <summary>
        /// Lưu tất cả thay đổi tích lũy xuống cơ sở dữ liệu.
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
