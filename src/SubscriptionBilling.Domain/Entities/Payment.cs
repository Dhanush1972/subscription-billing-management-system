using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Domain.Entities;

public class Payment : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public string? StripePaymentIntentId { get; set; }
    public string? StripeChargeId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string? PaymentMethodSummary { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? FailureMessage { get; set; }
    public DateTime PaymentDateUtc { get; set; } = DateTime.UtcNow;
}
