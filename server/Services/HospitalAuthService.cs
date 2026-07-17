using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public class HospitalAuthService : IHospitalAuthService
    {
        private readonly IHospitalHierarchyCache _hierarchyCache;

        public HospitalAuthService(IHospitalHierarchyCache hierarchyCache)
        {
            _hierarchyCache = hierarchyCache;
        }

        public async Task<List<int>?> GetPermittedHospitalIdsAsync(ClaimsPrincipal user)
        {
            // Nếu là Admin hoặc SuperAdmin, trả về null (có quyền truy cập toàn cục)
            if (user.IsInRole("Administrator") || user.IsInRole("SuperAdmin"))
            {
                return null; 
            }

            // Đọc Claim hospitalId của người dùng thường
            var hospitalIdStr = user.FindFirst("hospitalId")?.Value;
            if (string.IsNullOrEmpty(hospitalIdStr) || !int.TryParse(hospitalIdStr, out var hospitalId))
            {
                return new List<int>(); // Không có claim -> chặn không cho xem gì
            }

            return await GetDescendantHospitalIdsAsync(hospitalId);
        }

        public async Task<List<int>> GetDescendantHospitalIdsAsync(int hospitalId)
{
    // Nếu cache bị null hoặc chưa được nạp dữ liệu, trả về danh sách chỉ chứa chính ID đó thay vì crash 500
    if (_hierarchyCache == null)
    {
        return new List<int> { hospitalId };
    }

    var ids = _hierarchyCache.GetDescendantIds(hospitalId);
    return await Task.FromResult(ids ?? new List<int> { hospitalId });
}
    }
}