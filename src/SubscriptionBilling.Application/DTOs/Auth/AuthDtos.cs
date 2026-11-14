namespace SubscriptionBilling.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserProfileDto User
);

public record RefreshTokenRequest(
    string? RefreshToken
);

public record UserProfileDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? StripeCustomerId,
    IReadOnlyList<string> Roles
);
