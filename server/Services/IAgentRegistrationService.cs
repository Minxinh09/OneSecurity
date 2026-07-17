using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IAgentRegistrationService
    {
        Task<RegisterAgentResponse?> RegisterAsync(RegisterAgentRequest request);
    }
}
