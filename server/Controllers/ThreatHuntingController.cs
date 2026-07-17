using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/hunting")]
    public class ThreatHuntingController : ControllerBase
    {
        private readonly IThreatHuntingService _huntingService;

        public ThreatHuntingController(IThreatHuntingService huntingService)
        {
            _huntingService = huntingService;
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] ThreatSearchRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var results = await _huntingService.SearchAsync(request);
            return Ok(results);
        }
    }
}
