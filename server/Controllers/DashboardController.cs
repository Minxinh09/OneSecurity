using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Services;

using Microsoft.AspNetCore.Authorization;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DashboardSummaryResponse))]
        public async Task<IActionResult> GetSummary()
        {
            var summary = await _dashboardService.GetSummaryAsync();
            return Ok(summary);
        }

        [HttpGet("recent-alerts")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<RecentAlertDto>))]
        public async Task<IActionResult> GetRecentAlerts()
        {
            var alerts = await _dashboardService.GetRecentAlertsAsync();
            return Ok(alerts);
        }

        [HttpGet("recent-events")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<RecentEventDto>))]
        public async Task<IActionResult> GetRecentEvents()
        {
            var events = await _dashboardService.GetRecentEventsAsync();
            return Ok(events);
        }

        [HttpGet("agent-status")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<AgentStatusDto>))]
        public async Task<IActionResult> GetAgentStatus()
        {
            var statusList = await _dashboardService.GetAgentStatusListAsync();
            return Ok(statusList);
        }
    }
}
