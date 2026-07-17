using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Services;

using Microsoft.AspNetCore.Authorization;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/alerts")]
    [Authorize]
    public class AlertController : ControllerBase
    {
        private readonly IAlertManagementService _alertService;
        private readonly IAuditService _auditService;

        public AlertController(IAlertManagementService alertService, IAuditService auditService)
        {
            _alertService = alertService;
            _auditService = auditService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AlertListResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaged([FromQuery] AlertFilterRequest filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _alertService.GetPagedAsync(filter);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AlertDetailDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDetail(long id)
        {
            try
            {
                var response = await _alertService.GetDetailAsync(id);
                if (response == null)
                {
                    return NotFound(new { message = $"Alert with ID {id} not found." });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/acknowledge")]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Acknowledge(long id)
        {
            try
            {
                // Sử dụng tên người dùng từ Claims Identity nếu có, ngược lại mặc định là "Admin"
                var username = User.Identity?.Name ?? "Admin";

                var success = await _alertService.AcknowledgeAsync(id, username);
                if (!success)
                {
                    return NotFound(new { message = $"Alert with ID {id} not found." });
                }

                await _auditService.LogAsync("Acknowledge Alert", AuditResourceType.Alert, entityId: id.ToString(), description: $"Acknowledged alert {id}", success: true, statusCode: 200, severity: AuditSeverity.Information);

                return Ok(new { message = $"Alert with ID {id} acknowledged successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("acknowledge-many")]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AcknowledgeMany([FromBody] BulkAcknowledgeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var username = User.Identity?.Name ?? "Admin";

                var success = await _alertService.BulkAcknowledgeAsync(request.AlertIds, username);
                if (!success)
                {
                    return NotFound(new { message = "None of the provided alert IDs were found or processed." });
                }

                await _auditService.LogAsync("Bulk Acknowledge", AuditResourceType.Alert, description: $"Acknowledged alerts [{string.Join(", ", request.AlertIds)}]", success: true, statusCode: 200, severity: AuditSeverity.Information);

                return Ok(new { message = $"Alerts [{string.Join(", ", request.AlertIds)}] acknowledged successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
