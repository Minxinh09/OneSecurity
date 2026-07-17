using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.Data;
using OneSecurity.Server.Models;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/hospitals")]
    [Authorize]
    public class HospitalController : ControllerBase
    {
        private readonly LocalAgentDbContext _dbContext;
        private readonly IHospitalAuthService _hospitalAuthService;

        public HospitalController(LocalAgentDbContext dbContext, IHospitalAuthService hospitalAuthService)
        {
            _dbContext = dbContext;
            _hospitalAuthService = hospitalAuthService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Hospital>>> GetMyHospitals()
        {
            var permittedIds = await _hospitalAuthService.GetPermittedHospitalIdsAsync(User); //
            if (permittedIds == null)
            {
                // SuperAdmin/Admin -> Lấy toàn bộ danh sách bệnh viện
                var allHospitals = await _dbContext.Hospitals.IgnoreQueryFilters().ToListAsync();
                return Ok(allHospitals);
            }

            // User thường -> Chỉ lấy các bệnh viện nằm trong nhánh phân cấp của họ
            var hospitals = await _dbContext.Hospitals
                .Where(h => permittedIds.Contains(h.Id))
                .ToListAsync();
                
            return Ok(hospitals);
        }
    }
}