using System;
using System.Collections.Generic; // Thêm để hỗ trợ List<Claim>
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // <--- BẮT BUỘC PHẢI CÓ để dùng .Include() và .FirstOrDefaultAsync()
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OneSecurity.Server.Configuration;
using OneSecurity.Server.DTOs;
using OneSecurity.Server.Models;

namespace OneSecurity.Server.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<JwtOptions> jwtOptions,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            // Nạp thông tin Hospital liên kết để hiển thị chính xác ở Frontend
            var user = await _userManager.Users
                .Include(u => u.Hospital)
                .FirstOrDefaultAsync(u => u.UserName == request.Username);

            if (user == null || !user.IsActive) return null;

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Count > 0 ? roles[0] : "Viewer";

            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var tokenString = GenerateJwtToken(user, role);

            return new LoginResponse
            {
                Token = tokenString,
                Username = user.UserName ?? string.Empty,
                Role = role,
                FullName = user.FullName,
                HospitalId = user.HospitalId,
                HospitalName = user.Hospital?.Name // Trả về tên bệnh viện cho frontend
            };
        }

        public async Task<UserDto?> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userManager.FindByNameAsync(request.Username);
            if (existingUser != null)
            {
                _logger.LogWarning("Username {Username} is already registered", request.Username);
                return null;
            }

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = request.Email,
                FullName = request.FullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create user: {Errors}", string.Join(", ", createResult.Errors));
                return null;
            }

            // Assign role
            var roleName = request.Role;
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                roleName = "Viewer";
            }
            await _userManager.AddToRoleAsync(user, roleName);

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roleName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<UserDto?> GetCurrentUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Count > 0 ? roles[0] : "Viewer";

            return new UserDto
            {
                Id = user.Id,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (result.Succeeded)
            {
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
                return true;
            }

            _logger.LogWarning("Failed to change password for user {UserId}: {Errors}", userId, string.Join(", ", result.Errors));
            return false;
        }

        private string GenerateJwtToken(ApplicationUser user, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Fail-safe logic: priority environment variable -> dev fallback -> throw in production
            var secretKey = Environment.GetEnvironmentVariable("ONESECURITY_JWT_SECRET") ?? _jwtOptions.SecretKey;
            
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                // Find if we are in Development
                var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ||
                                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
                if (isDevelopment)
                {
                    _logger.LogWarning("ONESECURITY_JWT_SECRET environment variable is missing. Falling back to development hardcoded key!");
                    secretKey = "onesecurity_secret_jwt_key_2026_super_long_key_development_only";
                }
                else
                {
                    throw new InvalidOperationException("ONESECURITY_JWT_SECRET environment variable is missing in Production environment!");
                }
            }

            var key = Encoding.ASCII.GetBytes(secretKey);

            // 1. Tạo danh sách claims cơ bản
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, role)
            };

            // 2. Kiểm tra an toàn: Chỉ gán claim "hospitalId" (chữ thường) nếu user thực sự thuộc một bệnh viện
            if (user.HospitalId.HasValue)
            {
                claims.Add(new Claim("hospitalId", user.HospitalId.Value.ToString())); // Đã sửa tên thành "hospitalId" và tránh lỗi Null
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims), // Sử dụng list claims động đã xử lý ở trên
                Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpireMinutes),
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}