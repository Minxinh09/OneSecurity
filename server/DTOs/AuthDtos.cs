using System;

namespace OneSecurity.Server.DTOs
{
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class RegisterRequest
    {
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; } // Administrator, Operator, Viewer
    }

    public class LoginResponse
    {
        public required string Token { get; set; }
        public required string Username { get; set; }
        public required string Role { get; set; }
        public required string FullName { get; set; }
        
        // ======= THÊM HAI THUỘC TÍNH NÀY VÀO ĐÂY =======
        public int? HospitalId { get; set; }
        public string? HospitalName { get; set; }
        // ===============================================
    }

    public class UserDto
    {
        public required string Id { get; set; }
        public required string Username { get; set; }
        public required string Email { get; set; }
        public required string FullName { get; set; }
        public required string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string OldPassword { get; set; }
        public required string NewPassword { get; set; }
    }
}