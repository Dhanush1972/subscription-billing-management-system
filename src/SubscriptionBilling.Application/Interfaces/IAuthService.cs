using SubscriptionBilling.Application.DTOs.Auth;
using SubscriptionBilling.Application.DTOs.Users;
using SubscriptionBilling.Application.Common;

namespace SubscriptionBilling.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null);
    Task<AuthResponse> RefreshTokenAsync(string token, string? ipAddress = null);
    Task RevokeTokenAsync(string token, string? ipAddress = null);
    Task<UserProfileDto> GetCurrentUserProfileAsync(int userId);
    Task<PagedResult<UserManagementDto>> GetAllUsersAsync(int pageNumber = 1, int pageSize = 10);
    Task UpdateUserStatusAsync(int userId, bool isActive, int adminUserId);
}
