namespace SubscriptionBilling.Domain.Entities;

public class WebhookEvent : BaseEntity
{
    public string StripeEventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string Status { get; set; } = "Received"; // Received, Processed, Failed, Ignored
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
