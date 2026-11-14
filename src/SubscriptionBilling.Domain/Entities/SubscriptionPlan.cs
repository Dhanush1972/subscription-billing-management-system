using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "INR";
    public BillingInterval BillingInterval { get; set; } = BillingInterval.Monthly;
    public string? StripeProductId { get; set; }
    public string? StripePriceId { get; set; }
    public string FeaturesJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public bool IsPopular { get; set; } = false;

    // Navigation properties for normalized pricing and features
    public ICollection<SubscriptionPlanPrice> Prices { get; set; } = new List<SubscriptionPlanPrice>();
    public ICollection<SubscriptionPlanFeature> PlanFeatures { get; set; } = new List<SubscriptionPlanFeature>();

    // Navigation property
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
