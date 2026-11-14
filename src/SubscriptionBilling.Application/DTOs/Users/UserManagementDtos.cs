namespace SubscriptionBilling.Application.DTOs.Users;

public record UserManagementDto(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? StripeCustomerId,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc
);

public record UpdateUserStatusRequest(
    bool IsActive
);
