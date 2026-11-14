using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Domain.Entities;

public class SubscriptionPlanPrice : BaseEntity
{
    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public BillingInterval BillingInterval { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";

    public string? StripePriceId { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation property for subscriptions billed under this price
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
