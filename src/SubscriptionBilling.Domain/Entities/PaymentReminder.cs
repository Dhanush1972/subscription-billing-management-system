using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Domain.Entities;

public class PaymentReminder : BaseEntity
{
    public int SubscriptionId { get; set; }
    public Subscription Subscription { get; set; } = null!;

    public DateTime ScheduledForUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public ReminderType ReminderType { get; set; } = ReminderType.UpcomingRenewal;
    public PaymentReminderStatus Status { get; set; } = PaymentReminderStatus.Pending;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}
