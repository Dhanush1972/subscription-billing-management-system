using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Domain.Entities;

public class Subscription : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    // Normalized historical price reference
    public int? SubscriptionPlanPriceId { get; set; }
    public SubscriptionPlanPrice? SubscriptionPlanPrice { get; set; }

    public string? StripeSubscriptionId { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Incomplete;

    public DateTime CurrentPeriodStartUtc { get; set; }
    public DateTime CurrentPeriodEndUtc { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public DateTime? CanceledAtUtc { get; set; }

    // Navigation properties
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<PaymentReminder> PaymentReminders { get; set; } = new List<PaymentReminder>();
}
