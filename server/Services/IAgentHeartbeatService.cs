using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IAgentHeartbeatService
    {
        Task<HeartbeatResponse?> ProcessHeartbeatAsync(HeartbeatRequest request);
    }
}
