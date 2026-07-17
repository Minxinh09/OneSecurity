using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OneSecurity.Server.Models;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/assets")]
    [Authorize]
    public class AssetsController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetsController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InfrastructureAsset>>> GetAll()
        {
            var assets = await _assetService.GetAllAsync();
            return Ok(assets);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InfrastructureAsset>> GetById(long id)
        {
            var asset = await _assetService.GetByIdAsync(id);
            if (asset == null) return NotFound(new { message = "Asset not found" });
            return Ok(asset);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator,Operator")]
        public async Task<ActionResult<InfrastructureAsset>> Create([FromBody] InfrastructureAsset asset)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            try
            {
                if (!User.IsInRole("Administrator") && !User.IsInRole("SuperAdmin"))
                {
                    var hospitalIdStr = User.FindFirst("hospitalId")?.Value;
                    if (int.TryParse(hospitalIdStr, out var hospitalId))
                    {
                        asset.HospitalId = hospitalId;
                    }
                }

                var created = await _assetService.CreateAsync(asset);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator,Operator")]
        public async Task<ActionResult<InfrastructureAsset>> Update(long id, [FromBody] InfrastructureAsset asset)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _assetService.UpdateAsync(id, asset);
            if (updated == null) return NotFound(new { message = "Asset not found" });
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(long id)
        {
            var deleted = await _assetService.DeleteAsync(id);
            if (!deleted) return NotFound(new { message = "Asset not found" });
            return NoContent();
        }

        [HttpPost("{id}/status")]
        [Authorize(Roles = "Administrator,Operator")]
        public async Task<IActionResult> TransitionStatus(long id, [FromBody] string targetStatus)
        {
            var success = await _assetService.TransitionStatusAsync(id, targetStatus);
            if (!success) return BadRequest(new { message = "Invalid status transition or asset not found." });
            return Ok(new { message = "Status transitioned successfully." });
        }
    }
}
