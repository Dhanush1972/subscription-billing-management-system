namespace SubscriptionBilling.Domain.Entities;

public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? DetailsJson { get; set; }
    public string? IpAddress { get; set; }
}
