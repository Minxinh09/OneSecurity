using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IMetricService
    {
        Task<MetricResponse?> IngestMetricAsync(MetricRequest request);
    }
}
