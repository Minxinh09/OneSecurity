using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Services;
using OneSecurity.Server.Models.Enums;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuditService _auditService;

        public AuthController(IAuthService authService, IAuditService auditService)
        {
            _authService = authService;
            _auditService = auditService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request);
            if (response == null)
            {
                await _auditService.LogAsync("Login", AuditResourceType.Authentication, description: $"Failed login attempt for {request.Username}", success: false, statusCode: 401, severity: AuditSeverity.Warning, userNameOverride: request.Username, roleOverride: "Unknown");
                return Unauthorized(new { message = "Invalid username or password" });
            }
            await _auditService.LogAsync("Login", AuditResourceType.Authentication, description: "User logged in successfully", success: true, statusCode: 200, severity: AuditSeverity.Information, userNameOverride: response.Username, roleOverride: response.Role);
            return Ok(response);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var user = await _authService.RegisterAsync(request);
            if (user == null)
            {
                await _auditService.LogAsync("Register", AuditResourceType.Authentication, description: $"Failed register attempt for {request.Username}", success: false, statusCode: 400, severity: AuditSeverity.Warning, userNameOverride: request.Username, roleOverride: "Unknown");
                return BadRequest(new { message = "Username already exists or invalid parameters" });
            }
            await _auditService.LogAsync("Register", AuditResourceType.Authentication, entityId: user.Id, description: $"Registered new user {user.Username}", success: true, statusCode: 201, severity: AuditSeverity.Information, userNameOverride: user.Username, roleOverride: user.Role);
            return CreatedAtAction(nameof(GetCurrentUser), null, user);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _authService.ChangePasswordAsync(userId, request);
            if (!success)
            {
                await _auditService.LogAsync("Change Password", AuditResourceType.Authentication, entityId: userId, description: "Failed to change password", success: false, statusCode: 400, severity: AuditSeverity.Warning);
                return BadRequest(new { message = "Failed to change password. Make sure the old password is correct and the new password meets security requirements." });
            }
            await _auditService.LogAsync("Change Password", AuditResourceType.Authentication, entityId: userId, description: "Password changed successfully", success: true, statusCode: 200, severity: AuditSeverity.Warning);
            return Ok(new { message = "Password updated successfully" });
        }
    }
}
