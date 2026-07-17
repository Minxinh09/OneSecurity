using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;
using OneSecurity.Server.Services; // <--- QUAN TRỌNG: Dòng này sửa lỗi CS0246 trong Controller

namespace OneSecurity.Server.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHospitalAuthService _hospitalAuthService;

        public UserController(
            UserManager<ApplicationUser> userManager,
            IHospitalAuthService hospitalAuthService)
        {
            _userManager = userManager;
            _hospitalAuthService = hospitalAuthService;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator,Operator,SecurityOperator")]
        public async Task<IActionResult> GetAllUsers()
        {
            // KHẮC PHỤC LỖI CS0266: Khai báo kiểu chung IQueryable một cách tường minh
            IQueryable<ApplicationUser> query = _userManager.Users.Include(u => u.Hospital);
            
            var permittedIds = await _hospitalAuthService.GetPermittedHospitalIdsAsync(User);
            if (permittedIds != null)
            {
                query = query.Where(u => u.HospitalId.HasValue && permittedIds.Contains(u.HospitalId.Value));
            }

            var users = await query.ToListAsync();

            var userDtoTasks = users.Select(async u =>
            {
                var roles = await _userManager.GetRolesAsync(u);
                return new UserDto
                {
                    Id = u.Id,
                    Username = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    FullName = u.FullName,
                    Role = roles.Count > 0 ? roles[0] : "Viewer",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                };
            });

            var result = await Task.WhenAll(userDtoTasks);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isNewAdmin = User.IsInRole("Administrator");

            if (currentUserId != id && !isNewAdmin)
            {
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new UserDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.Count > 0 ? roles[0] : "Viewer",
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }
    }
}