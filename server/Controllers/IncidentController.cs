using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Repositories;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/incidents")]
    [Authorize]
    public class IncidentController : ControllerBase
    {
        private readonly IIncidentService _incidentService;
        private readonly IIncidentRepository _incidentRepository;
        private readonly OneSecurity.Server.Data.LocalAgentDbContext _context;

        public IncidentController(
            IIncidentService incidentService, 
            IIncidentRepository incidentRepository,
            OneSecurity.Server.Data.LocalAgentDbContext context)
        {
            _incidentService = incidentService;
            _incidentRepository = incidentRepository;
            _context = context;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IncidentListResponse))]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? severity = null,
            [FromQuery] string? assignedUserId = null,
            [FromQuery] string? searchQuery = null)
        {
            var response = await _incidentRepository.GetPagedAsync(page, pageSize, status, severity, assignedUserId, searchQuery);
            return Ok(response);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IncidentDetailDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id)
        {
            var incident = await _incidentRepository.GetByIdAsync(id);
            if (incident == null)
            {
                return NotFound(new { message = $"Incident with ID {id} not found." });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Administrator") || User.IsInRole("SuperAdmin");
            
            var permittedHospitals = _context.PermittedHospitalIds;
            bool hasAccess = isAdmin || 
                             incident.AssignedUserId == currentUserId || 
                             (permittedHospitals != null && incident.Alerts.Any(al => al.Agent != null && al.Agent.HospitalId.HasValue && permittedHospitals.Contains(al.Agent.HospitalId.Value)));
                             
            if (!hasAccess)
            {
                return NotFound(new { message = $"Incident with ID {id} not found." });
            }

            // Map manually for read-only retrieval without changing tracked state
            var detailDto = new IncidentDetailDto
            {
                Id = incident.Id,
                Title = incident.Title,
                Description = incident.Description,
                Severity = incident.Severity.ToString(),
                Status = incident.Status.ToString(),
                AssignedUserId = incident.AssignedUserId,
                AssignedUserName = incident.AssignedUser?.UserName,
                AssignedAt = incident.AssignedAt,
                CreatedAt = incident.CreatedAt,
                UpdatedAt = incident.UpdatedAt,
                AlertCount = incident.Alerts.Count,
                CreatedBy = incident.CreatedBy?.UserName,
                ResolvedAt = incident.ResolvedAt,
                ClosedAt = incident.ClosedAt,
                Alerts = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(incident.Alerts, a => new RecentAlertDto
                {
                    Id = a.Id,
                    AgentId = a.AgentId,
                    AgentHostname = a.Agent?.Hostname,
                    RuleName = a.RuleName,
                    Severity = a.Severity,
                    Title = a.Title,
                    Message = a.Message,
                    Category = a.Category,
                    CreatedAt = a.CreatedAt,
                    IsAcknowledged = a.IsAcknowledged
                }))
            };

            return Ok(detailDto);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(IncidentDetailDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateIncidentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _incidentService.CreateAsync(request, userId);
                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("{id}/assign")]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IncidentDetailDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Assign(long id, [FromBody] AssignIncidentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _incidentService.AssignAsync(id, request, userId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IncidentDetailDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateIncidentStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _incidentService.UpdateStatusAsync(id, request, userId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("{id}/alerts")]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IncidentDetailDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LinkAlerts(long id, [FromBody] LinkAlertsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _incidentService.LinkAlertsAsync(id, request, userId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}/alerts/{alertId}")]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IncidentDetailDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnlinkAlert(long id, long alertId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            try
            {
                var response = await _incidentService.UnlinkAlertAsync(id, alertId, userId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
