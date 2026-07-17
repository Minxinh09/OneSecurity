using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IThreatHuntingService
    {
        Task<ThreatSearchResultDto> SearchAsync(ThreatSearchRequest request);
    }
}
