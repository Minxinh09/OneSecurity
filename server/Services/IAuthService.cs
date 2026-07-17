using System.Threading.Tasks;
using OneSecurity.Server.DTOs;

namespace OneSecurity.Server.Services
{
    public interface IAuthService
    {
        Task<LoginResponse?> LoginAsync(LoginRequest request);
        Task<UserDto?> RegisterAsync(RegisterRequest request);
        Task<UserDto?> GetCurrentUserAsync(string userId);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    }
}
