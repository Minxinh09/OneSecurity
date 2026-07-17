using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Services;

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/collectors")]
    [Authorize]
    public class CollectorsController : ControllerBase
    {
        private readonly ICollectorService _collectorService;

        public CollectorsController(ICollectorService collectorService)
        {
            _collectorService = collectorService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CollectorNode>>> GetAll()
        {
            var collectors = await _collectorService.GetAllAsync();
            return Ok(collectors);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CollectorNode>> GetById(long id)
        {
            var collector = await _collectorService.GetByIdAsync(id);
            if (collector == null) return NotFound(new { message = "Collector not found" });
            return Ok(collector);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<CollectorNode>> Create([FromBody] CollectorNode collector)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var created = await _collectorService.CreateAsync(collector);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<CollectorNode>> Update(long id, [FromBody] CollectorNode collector)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _collectorService.UpdateAsync(id, collector);
            if (updated == null) return NotFound(new { message = "Collector not found" });
            return Ok(updated);
        }

        [HttpGet("{id}/sync")]
        [AllowAnonymous] // Collector Nodes authenticate using custom header key instead of user session JWT
        public async Task<ActionResult<CollectorSyncData>> SyncCollector(
            long id, 
            [FromQuery] int configVersion, 
            [FromQuery] int rulesVersion)
        {
            if (!Request.Headers.TryGetValue("X-Collector-Secret", out var secrets) || string.IsNullOrEmpty(secrets))
            {
                return Unauthorized(new { message = "X-Collector-Secret header is missing." });
            }

            var secret = secrets.ToString();
            var syncData = await _collectorService.SyncCollectorAsync(id, secret, configVersion, rulesVersion);
            if (syncData == null)
            {
                return Unauthorized(new { message = "Invalid collector credentials." });
            }

            return Ok(syncData);
        }
    }
}
