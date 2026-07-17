using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.Models;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/enrollment")]
    [Authorize]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;

        public EnrollmentController(IEnrollmentService enrollmentService)
        {
            _enrollmentService = enrollmentService;
        }

        [HttpGet("tokens")]
        public async Task<ActionResult<IEnumerable<EnrollmentToken>>> GetAllTokens()
        {
            var tokens = await _enrollmentService.GetAllTokensAsync();
            return Ok(tokens);
        }

        [HttpGet("tokens/{id}")]
        public async Task<ActionResult<EnrollmentToken>> GetTokenDetails(long id)
        {
            var token = await _enrollmentService.GetTokenDetailsAsync(id);
            if (token == null) return NotFound(new { message = "Token not found" });
            return Ok(token);
        }

        [HttpPost("generate")]
        [Authorize(Roles = "Administrator,Operator")]
        public async Task<ActionResult<EnrollmentToken>> GenerateToken([FromBody] GenerateEnrollmentTokenDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var createdBy = User.Identity?.Name ?? "User";
                var token = await _enrollmentService.GenerateTokenAsync(
                    request.AssetId, 
                    request.PolicyId, 
                    request.CollectorId, 
                    request.MaxUses, 
                    request.Reason, 
                    createdBy
                );
                return CreatedAtAction(nameof(GetTokenDetails), new { id = token.Id }, token);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }

    public class GenerateEnrollmentTokenDto
    {
        public long AssetId { get; set; }
        public long PolicyId { get; set; }
        public long CollectorId { get; set; }
        public int MaxUses { get; set; } = 1;
        public string? Reason { get; set; }
    }
}
