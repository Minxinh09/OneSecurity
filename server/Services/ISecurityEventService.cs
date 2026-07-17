using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface ISecurityEventService
    {
        Task<SecurityEventResponse?> IngestEventAsync(SecurityEventRequest request);
    }
}
