using SubscriptionBilling.Application.DTOs.Plans;
using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Application.DTOs.Subscriptions;

public record SubscriptionDto(
    int Id,
    int UserId,
    string UserEmail,
    int PlanId,
    SubscriptionPlanDto Plan,
    int? SubscriptionPlanPriceId,
    string? StripeSubscriptionId,
    SubscriptionStatus Status,
    DateTime CurrentPeriodStartUtc,
    DateTime CurrentPeriodEndUtc,
    bool CancelAtPeriodEnd,
    DateTime? CanceledAtUtc,
    DateTime CreatedAtUtc
);

public record CreateCheckoutSessionRequest(
    int PlanId,
    string SuccessUrl,
    string CancelUrl,
    BillingInterval? BillingInterval = null,
    int? PriceId = null
);

public record CheckoutSessionResponse(
    string SessionId,
    string CheckoutUrl
);

public record UpgradeSubscriptionRequest(
    int NewPlanId,
    int? NewPriceId = null
);

public record CancelSubscriptionRequest(
    bool Immediately = false
);
