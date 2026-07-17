using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Repositories;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/audit")] // Simplified route to match spec GET /api/v1/audit
    [Authorize(Roles = "Administrator")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditRepository _auditRepository;

        public AuditController(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogFilterRequest filter)
        {
            if (filter == null)
            {
                filter = new AuditLogFilterRequest();
            }
            
            var result = await _auditRepository.GetPagedAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuditLog(int id)
        {
            var auditLog = await _auditRepository.GetByIdAsync(id);
            if (auditLog == null)
            {
                return NotFound();
            }
            return Ok(auditLog);
        }
    }
}
