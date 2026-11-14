using SubscriptionBilling.Domain.Entities;

namespace SubscriptionBilling.Application.Interfaces;

public interface IPdfInvoiceGenerator
{
    byte[] GenerateInvoicePdf(Invoice invoice, User user, SubscriptionPlan? plan);
}

public interface IEmailService
{
    Task SendPaymentSuccessNotificationAsync(string toEmail, string customerName, string invoiceNumber, decimal amount, string currency);
    Task SendPaymentFailedNotificationAsync(string toEmail, string customerName, string invoiceNumber, decimal amount, string currency, string? reason);
    Task SendRenewalReminderAsync(string toEmail, string customerName, string planName, DateTime renewalDate, decimal amount, string currency);
}

public interface IAuditLogService
{
    Task LogAsync(int? userId, string action, string entityName, string? entityId, object? details = null, string? ipAddress = null);
}

public interface IPaymentReminderProcessor
{
    Task ProcessUpcomingRenewalsAsync(CancellationToken cancellationToken = default);
    Task ProcessFailedPaymentRetriesAsync(CancellationToken cancellationToken = default);
}
