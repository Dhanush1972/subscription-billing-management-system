using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Domain.Entities;

public class Invoice : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
    public string? StripeInvoiceId { get; set; }
    public string BillingPeriod { get; set; } = string.Empty;
    public DateTime InvoiceDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime DueDateUtc { get; set; } = DateTime.UtcNow.AddDays(7);
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "usd";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? PdfFilePath { get; set; }

    // Navigation property
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
