using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using OneSecurity.Server.Services; // <--- QUAN TRỌNG: Dòng này sửa lỗi CS0246 trong Hub

namespace OneSecurity.Server.Realtime
{
    [Authorize]
    public class NotificationHub : Hub<INotificationHub>
    {
        private readonly IHospitalAuthService _hospitalAuthService;
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(IHospitalAuthService hospitalAuthService, ILogger<NotificationHub> logger)
        {
            _hospitalAuthService = hospitalAuthService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);

            var user = Context.User;
            if (user != null && user.Identity?.IsAuthenticated == true)
            {
                var permittedIds = await _hospitalAuthService.GetPermittedHospitalIdsAsync(user);
                if (permittedIds == null)
                {
                    // SuperAdmin/Admin gia nhập nhóm quản trị toàn cục
                    await Groups.AddToGroupAsync(Context.ConnectionId, "SuperAdmins");
                }
                else
                {
                    // User thường gia nhập nhóm phân cấp bệnh viện của họ
                    foreach (var id in permittedIds)
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"Hospital_{id}");
                    }
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}, Exception: {Exception}", Context.ConnectionId, exception?.Message ?? "None");
            await base.OnDisconnectedAsync(exception);
        }

        public Task JoinDashboard()
        {
            return Task.CompletedTask;
        }
    }
}