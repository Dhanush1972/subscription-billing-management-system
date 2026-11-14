using Microsoft.Extensions.Logging;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Infrastructure.Data;
using System.Text.Json;

namespace SubscriptionBilling.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPaymentSuccessNotificationAsync(string toEmail, string customerName, string invoiceNumber, decimal amount, string currency)
    {
        _logger.LogInformation(
            "EMAIL SENT [Payment Succeeded] to '{Email}' (Recipient: {Name}): Invoice #{InvoiceNumber} paid successfully for {Amount} {Currency}.",
            toEmail, customerName, invoiceNumber, amount, currency.ToUpperInvariant());
        return Task.CompletedTask;
    }

    public Task SendPaymentFailedNotificationAsync(string toEmail, string customerName, string invoiceNumber, decimal amount, string currency, string? reason)
    {
        _logger.LogWarning(
            "EMAIL SENT [Payment Failed] to '{Email}' (Recipient: {Name}): Invoice #{InvoiceNumber} for {Amount} {Currency} failed. Reason: {Reason}.",
            toEmail, customerName, invoiceNumber, amount, currency.ToUpperInvariant(), reason ?? "Card declined or insufficient funds");
        return Task.CompletedTask;
    }

    public Task SendRenewalReminderAsync(string toEmail, string customerName, string planName, DateTime renewalDate, decimal amount, string currency)
    {
        _logger.LogInformation(
            "EMAIL SENT [Upcoming Renewal] to '{Email}' (Recipient: {Name}): Plan '{PlanName}' renews on {RenewalDate:yyyy-MM-dd} for {Amount} {Currency}.",
            toEmail, customerName, planName, renewalDate, amount, currency.ToUpperInvariant());
        return Task.CompletedTask;
    }
}

public class AuditLogService : IAuditLogService
{
    private readonly SubscriptionBillingDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(SubscriptionBillingDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(int? userId, string action, string entityName, string? entityId, object? details = null, string? ipAddress = null)
    {
        try
        {
            var audit = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                DetailsJson = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = ipAddress,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _context.AuditLogs.AddAsync(audit);
            await _context.SaveChangesAsync();

            _logger.LogInformation("AUDIT: User {UserId} executed {Action} on {EntityName} ({EntityId})", userId, action, entityName, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit log for action {Action} on entity {EntityName}", action, entityName);
        }
    }
}
