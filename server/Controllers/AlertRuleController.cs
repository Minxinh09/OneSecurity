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
    [Route("api/v1/alert-rules")]
    [Authorize]
    public class AlertRuleController : ControllerBase
    {
        private readonly IAlertRuleManagementService _ruleService;
        private readonly IAuditService _auditService;

        public AlertRuleController(IAlertRuleManagementService ruleService, IAuditService auditService)
        {
            _ruleService = ruleService;
            _auditService = auditService;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AlertRuleListResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPaged([FromQuery] AlertRuleFilterRequest filter)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _ruleService.GetPagedAsync(filter);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AlertRuleDetailDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDetail(long id)
        {
            try
            {
                var response = await _ruleService.GetDetailAsync(id);
                if (response == null)
                {
                    return NotFound(new { message = $"AlertRule with ID {id} not found." });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(AlertRuleDetailDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateAlertRuleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _ruleService.CreateAsync(request);
                await _auditService.LogAsync("Create Rule", AuditResourceType.Rule, entityId: response!.Id.ToString(), description: $"Created rule {request.Name}", success: true, statusCode: 201, severity: AuditSeverity.Information);
                return CreatedAtAction(nameof(GetDetail), new { id = response!.Id }, response);
            }
            catch (ArgumentException ex) // Lỗi định dạng JSON
            {
                await _auditService.LogAsync("Create Rule", AuditResourceType.Rule, description: $"Failed to create rule {request.Name}: {ex.Message}", success: false, statusCode: 400, severity: AuditSeverity.Warning);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) // Lỗi trùng lặp tên
            {
                await _auditService.LogAsync("Create Rule", AuditResourceType.Rule, description: $"Failed to create rule {request.Name}: {ex.Message}", success: false, statusCode: 409, severity: AuditSeverity.Warning);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AlertRuleDetailDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateAlertRuleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var response = await _ruleService.UpdateAsync(id, request);
                if (response == null)
                {
                    return NotFound(new { message = $"AlertRule with ID {id} not found." });
                }

                await _auditService.LogAsync("Update Rule", AuditResourceType.Rule, entityId: id.ToString(), description: $"Updated rule {request.Name}", success: true, statusCode: 200, severity: AuditSeverity.Information);
                return Ok(response);
            }
            catch (ArgumentException ex) // Lỗi định dạng JSON
            {
                await _auditService.LogAsync("Update Rule", AuditResourceType.Rule, entityId: id.ToString(), description: $"Failed to update rule {request.Name}: {ex.Message}", success: false, statusCode: 400, severity: AuditSeverity.Warning);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) // Lỗi trùng lặp tên
            {
                await _auditService.LogAsync("Update Rule", AuditResourceType.Rule, entityId: id.ToString(), description: $"Failed to update rule {request.Name}: {ex.Message}", success: false, statusCode: 409, severity: AuditSeverity.Warning);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/enable")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Enable(long id)
        {
            try
            {
                var success = await _ruleService.EnableAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"AlertRule with ID {id} not found." });
                }

                await _auditService.LogAsync("Enable Rule", AuditResourceType.Rule, entityId: id.ToString(), description: $"Enabled rule {id}", success: true, statusCode: 200, severity: AuditSeverity.Information);
                return Ok(new { message = $"AlertRule with ID {id} enabled successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("{id}/disable")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Disable(long id)
        {
            try
            {
                var success = await _ruleService.DisableAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"AlertRule with ID {id} not found." });
                }

                await _auditService.LogAsync("Disable Rule", AuditResourceType.Rule, entityId: id.ToString(), description: $"Disabled rule {id}", success: true, statusCode: 200, severity: AuditSeverity.Warning);
                return Ok(new { message = $"AlertRule with ID {id} disabled successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var success = await _ruleService.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"AlertRule with ID {id} not found." });
                }

                await _auditService.LogAsync("Delete Rule", AuditResourceType.Rule, entityId: id.ToString(), description: $"Deleted rule {id}", success: true, statusCode: 200, severity: AuditSeverity.Critical);
                return Ok(new { message = $"AlertRule with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}
