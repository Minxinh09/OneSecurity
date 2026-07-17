using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/v1/dashboard")]
    public class DashboardSummaryController : ControllerBase
    {
        private readonly IOverviewService _overviewService;
        private readonly ITimelineService _timelineService;

        public DashboardSummaryController(IOverviewService overviewService, ITimelineService timelineService)
        {
            _overviewService = overviewService;
            _timelineService = timelineService;
        }

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var overview = await _overviewService.GetOverviewAsync(userId);
            return Ok(overview);
        }

        [HttpGet("timeline")]
        public async Task<IActionResult> GetTimeline([FromQuery] DateTime? from)
        {
            var timeline = await _timelineService.GetUnifiedTimelineAsync(from);
            return Ok(timeline);
        }
    }
}
