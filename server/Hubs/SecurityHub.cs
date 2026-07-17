using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace OneSecurity.Server.Hubs
{
    public class SecurityHub : Hub
    {
        // Hub methods if clients need to invoke actions directly, 
        // but mostly we will push events server -> client.
        public async Task JoinDashboard()
        {
            var user = Context.User;
            if (user == null || user.Identity?.IsAuthenticated != true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "DashboardClients");
                return;
            }

            var role = user.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            var hospitalIdStr = user.FindFirst("hospitalId")?.Value;
            int? hospitalId = int.TryParse(hospitalIdStr, out var id) ? id : null;

            if (role == "SuperAdmin")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "SuperAdmins");
            }
            else if (role == "HospitalAdmin" && hospitalId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Hospital_{hospitalId.Value}");
            }
        }
    }
}
