using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace OneSecurity.Server.Services
{
    public interface IHospitalAuthService
    {
        Task<List<int>?> GetPermittedHospitalIdsAsync(ClaimsPrincipal user);
        Task<List<int>> GetDescendantHospitalIdsAsync(int hospitalId);
    }
}