using System.Threading.Tasks;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Gửi thông báo cảnh báo (Alert) đến kênh Telegram cấu hình.
        /// </summary>
        Task SendAlertAsync(Alert alert);
    }
}
