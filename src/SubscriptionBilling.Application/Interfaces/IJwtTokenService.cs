using SubscriptionBilling.Domain.Entities;

namespace SubscriptionBilling.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    RefreshToken GenerateRefreshToken(int userId, string? ipAddress = null);
}
