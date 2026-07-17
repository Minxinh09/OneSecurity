using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Services;
using OneSecurity.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/agent")]
    [Route("api/v1/collector")] // Alias for Go Agent compatibility
    public class AgentController : ControllerBase
    {
        private readonly IAgentRegistrationService _registrationService;
        private readonly IAgentHeartbeatService _heartbeatService;
        private readonly IMetricService _metricService;
        private readonly ISecurityEventService _eventService;
        private readonly LocalAgentDbContext _dbContext;

        public AgentController(
            IAgentRegistrationService registrationService,
            IAgentHeartbeatService heartbeatService,
            IMetricService metricService,
            ISecurityEventService eventService,
            LocalAgentDbContext dbContext)
        {
            _registrationService = registrationService;
            _heartbeatService = heartbeatService;
            _metricService = metricService;
            _eventService = eventService;
            _dbContext = dbContext;
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(RegisterAgentResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterAgentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _registrationService.RegisterAsync(request);
                if (response == null)
                {
                    return Conflict(new { message = $"Agent with hostname '{request.Hostname}' already exists." });
                }

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("heartbeat")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HeartbeatResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _heartbeatService.ProcessHeartbeatAsync(request);
                if (response == null)
                {
                    return NotFound(new { message = $"Agent with ID '{request.AgentId}' not found." });
                }

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("metrics")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MetricResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> IngestMetrics([FromBody] MetricRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _metricService.IngestMetricAsync(request);
                if (response == null)
                {
                    return NotFound(new { message = $"Agent with ID '{request.AgentId}' not found." });
                }

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("events")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SecurityEventResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> IngestEvents([FromBody] SecurityEventRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _eventService.IngestEventAsync(request);
                if (response == null)
                {
                    return NotFound(new { message = $"Agent with ID '{request.AgentId}' not found." });
                }

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("heartbeat/batch")]
        public async Task<IActionResult> ProcessHeartbeatBatch([FromBody] System.Collections.Generic.List<EnrichedHeartbeatDto> batch)
        {
            if (batch == null || batch.Count == 0) return Ok();

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in batch)
                {
                    var req = new HeartbeatRequest { AgentId = item.AgentId };
                    await _heartbeatService.ProcessHeartbeatAsync(req);
                }
                await transaction.CommitAsync();
                return Ok(new { processedCount = batch.Count });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("metrics/batch")]
        public async Task<IActionResult> IngestMetricsBatch([FromBody] System.Collections.Generic.List<EnrichedMetricDto> batch)
        {
            if (batch == null || batch.Count == 0) return Ok();

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in batch)
                {
                    var req = new MetricRequest
                    {
                        AgentId = item.AgentId,
                        CpuUsagePercent = item.CpuUsagePercent,
                        RamUsagePercent = item.RamUsagePercent,
                        DiskUsagePercent = item.DiskUsagePercent,
                        NetworkInBytes = item.NetworkInBytes,
                        NetworkOutBytes = item.NetworkOutBytes
                    };
                    await _metricService.IngestMetricAsync(req);
                }
                await transaction.CommitAsync();
                return Ok(new { processedCount = batch.Count });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("events/batch")]
        public async Task<IActionResult> IngestEventsBatch([FromBody] System.Collections.Generic.List<EnrichedSecurityEventDto> batch)
        {
            if (batch == null || batch.Count == 0) return Ok();

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in batch)
                {
                    var req = new SecurityEventRequest
                    {
                        EventId = item.EventId,
                        AgentId = item.AgentId,
                        Category = item.Category,
                        Severity = item.Severity,
                        Source = item.Source,
                        Title = item.Title,
                        Details = item.Details,
                        RawData = item.RawData
                    };
                    await _eventService.IngestEventAsync(req);
                }
                await transaction.CommitAsync();
                return Ok(new { processedCount = batch.Count });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("commands")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPendingCommand([FromQuery] string agentId)
        {
            if (string.IsNullOrEmpty(agentId))
            {
                return BadRequest("Missing agentId parameter.");
            }

            var action = await _dbContext.ResponseActions
                .Where(r => r.AgentId == agentId && r.Status == Models.Enums.ResponseStatus.Queued)
                .OrderBy(r => r.RequestedAt)
                .FirstOrDefaultAsync();

            if (action == null)
            {
                return NoContent();
            }

            // Update status to Executing via service to trigger SignalR & Audit
            var responseService = HttpContext.RequestServices.GetRequiredService<IResponseService>();
            await responseService.UpdateExecutionStatusAsync(action.CorrelationId, "Executing", "Agent is starting execution.");

            var commandDto = new AgentCommandDto
            {
                CommandId = action.CorrelationId,
                AgentId = action.AgentId,
                ActionType = action.ActionType.ToString(),
                Metadata = action.Metadata
            };

            return Ok(commandDto);
        }

        public class CommandResultRequest
        {
            public required string CommandId { get; set; }
            public required string Status { get; set; }
            public string? Message { get; set; }
        }

        [HttpPost("commands/result")]
        [AllowAnonymous]
        public async Task<IActionResult> ReportResult([FromBody] CommandResultRequest result)
        {
            if (result == null || string.IsNullOrEmpty(result.CommandId))
            {
                return BadRequest("Invalid result payload.");
            }

            var responseService = HttpContext.RequestServices.GetRequiredService<IResponseService>();
            var success = await responseService.UpdateExecutionStatusAsync(result.CommandId, result.Status, result.Message);
            if (!success)
            {
                return NotFound(new { message = $"Response action for CorrelationId {result.CommandId} not found." });
            }

            return Ok(new { success = true });
        }
    }
}
