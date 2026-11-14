namespace SubscriptionBilling.Domain.Enums;

public enum SubscriptionStatus
{
    Incomplete = 1,
    Active = 2,
    PastDue = 3,
    Canceled = 4,
    Unpaid = 5,
    Trialing = 6,
    Paused = 7
}
