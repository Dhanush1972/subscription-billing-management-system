namespace SubscriptionBilling.Domain.Enums;

public enum PaymentReminderStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}

public enum ReminderType
{
    UpcomingRenewal = 1,
    PaymentFailed = 2
}
