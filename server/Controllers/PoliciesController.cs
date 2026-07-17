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
    [Route("api/v1/policies")]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PoliciesController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AgentPolicy>>> GetAll()
        {
            var policies = await _policyService.GetAllAsync();
            return Ok(policies);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<AgentPolicy>> GetById(long id)
        {
            var policy = await _policyService.GetByIdAsync(id);
            if (policy == null) return NotFound(new { message = "Policy not found" });
            return Ok(policy);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<AgentPolicy>> Create([FromBody] AgentPolicy policy)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var created = await _policyService.CreateAsync(policy);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<AgentPolicy>> Update(long id, [FromBody] AgentPolicy policy)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _policyService.UpdateAsync(id, policy);
            if (updated == null) return NotFound(new { message = "Policy not found" });
            return Ok(updated);
        }
    }
}
