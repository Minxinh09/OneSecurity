using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    public class CommandCallbackRequest
    {
        public required string CommandId { get; set; } // Map to CorrelationId
        public required string Status { get; set; } // Executing, Succeeded, Failed
        public string? Message { get; set; }
    }

    [ApiController]
    [Route("api/v1/responses")]
    public class ResponsesController : ControllerBase
    {
        private readonly IResponseService _responseService;
        private readonly IConfiguration _configuration;

        public ResponsesController(IResponseService responseService, IConfiguration configuration)
        {
            _responseService = responseService;
            _configuration = configuration;
        }

        [HttpPost("request")]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        public async Task<IActionResult> RequestAction([FromBody] RequestResponseActionRequest request)
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
                var result = await _responseService.RequestActionAsync(request, userId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ApproveAction(long id)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _responseService.ApproveActionAsync(id, adminUserId);
                if (result == null)
                {
                    return NotFound(new { message = $"Response action with ID {id} not found." });
                }
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CancelAction(long id)
        {
            var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(adminUserId))
            {
                return Unauthorized();
            }

            try
            {
                var result = await _responseService.CancelActionAsync(id, adminUserId);
                if (result == null)
                {
                    return NotFound(new { message = $"Response action with ID {id} not found." });
                }
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] string? actionType = null,
            [FromQuery] string? agentId = null)
        {
            var result = await _responseService.GetPagedAsync(page, pageSize, status, actionType, agentId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _responseService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"Response action with ID {id} not found." });
            }
            return Ok(result);
        }

        [HttpGet("incident/{incidentId}")]
        [Authorize]
        public async Task<IActionResult> GetByIncidentId(long incidentId)
        {
            var result = await _responseService.GetByIncidentIdAsync(incidentId);
            return Ok(result);
        }

        [HttpPost("callback")]
        [AllowAnonymous] // Authenticated via custom API Key from Collector
        public async Task<IActionResult> CommandCallback([FromBody] CommandCallbackRequest request)
        {
            if (!IsCollectorAuthorized())
            {
                return Unauthorized("Invalid X-Api-Key.");
            }

            if (request == null || string.IsNullOrEmpty(request.CommandId))
            {
                return BadRequest("Invalid callback payload.");
            }

            var success = await _responseService.UpdateExecutionStatusAsync(request.CommandId, request.Status, request.Message);
            if (!success)
            {
                return NotFound(new { message = $"Response action for CorrelationId {request.CommandId} not found." });
            }

            return Ok(new { success = true });
        }

        private bool IsCollectorAuthorized()
        {
            var expectedKey = _configuration["Collector:ApiKey"] ?? "onesecurity_secret_key_2026";
            
            if (Request.Headers.TryGetValue("X-Api-Key", out var key))
            {
                return key.ToString() == expectedKey;
            }
            return false;
        }
    }
}
