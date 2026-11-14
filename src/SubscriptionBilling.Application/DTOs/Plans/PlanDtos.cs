using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Application.DTOs.Plans;

public record PlanPriceDto(
    int Id,
    int SubscriptionPlanId,
    BillingInterval BillingInterval,
    decimal Amount,
    string Currency,
    string? StripePriceId,
    bool IsActive
);

public record PlanFeatureDto(
    int Id,
    int SubscriptionPlanId,
    string FeatureName,
    string FeatureValue,
    int DisplayOrder
);

public record SubscriptionPlanDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    BillingInterval BillingInterval,
    string? StripePriceId,
    string? StripeProductId,
    IReadOnlyList<string> Features,
    bool IsActive,
    bool IsPopular,
    IReadOnlyList<PlanPriceDto> Prices,
    IReadOnlyList<PlanFeatureDto> PlanFeatures,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CreatePlanPriceRequest(
    BillingInterval BillingInterval,
    decimal Amount,
    string Currency,
    string? StripePriceId
);

public record CreatePlanFeatureRequest(
    string FeatureName,
    string FeatureValue,
    int DisplayOrder
);

public record CreatePlanRequest(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    BillingInterval BillingInterval,
    bool IsPopular,
    List<string> Features,
    List<CreatePlanPriceRequest>? Prices = null,
    List<CreatePlanFeatureRequest>? PlanFeatures = null
);

public record UpdatePlanRequest(
    string Name,
    string Description,
    bool IsActive,
    bool IsPopular,
    List<string> Features,
    List<CreatePlanPriceRequest>? Prices = null,
    List<CreatePlanFeatureRequest>? PlanFeatures = null
);
