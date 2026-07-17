using System.Collections.Generic;
using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IThreatSearchProvider
    {
        string SourceType { get; }
        Task<List<ThreatSearchResultItemDto>> SearchAsync(ThreatSearchRequest request);
    }
}
