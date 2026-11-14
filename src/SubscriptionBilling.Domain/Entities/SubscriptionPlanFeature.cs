namespace SubscriptionBilling.Domain.Entities;

public class SubscriptionPlanFeature : BaseEntity
{
    public int SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public string FeatureName { get; set; } = string.Empty;
    public string FeatureValue { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
