using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Application.Services;

public interface IStripeWebhookService
{
    Task ProcessWebhookEventAsync(string payloadJson, string signatureHeader, string webhookSecret);
}

public class StripeWebhookService : IStripeWebhookService
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        IApplicationDbContext context,
        IStripePaymentGateway stripeGateway,
        IEmailService emailService,
        IAuditLogService auditLogService,
        ILogger<StripeWebhookService> logger)
    {
        _context = context;
        _stripeGateway = stripeGateway;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task ProcessWebhookEventAsync(string payloadJson, string signatureHeader, string webhookSecret)
    {
        // 1. Verify Webhook Signature
        var isValid = await _stripeGateway.VerifyWebhookSignatureAsync(payloadJson, signatureHeader, webhookSecret);
        if (!isValid)
        {
            _logger.LogWarning("Rejecting Stripe Webhook: Signature verification failed.");
            throw new UnauthorizedAccessException("Invalid webhook signature.");
        }

        // 2. Parse Event JSON
        using var jsonDoc = JsonDocument.Parse(payloadJson);
        var root = jsonDoc.RootElement;
        var eventId = root.GetProperty("id").GetString() ?? "";
        var eventType = root.GetProperty("type").GetString() ?? "";

        // 3. Idempotency Check
        var existingEvent = await _context.WebhookEvents.FirstOrDefaultAsync(w => w.StripeEventId == eventId);
        if (existingEvent != null)
        {
            _logger.LogInformation("Webhook event {EventId} ({EventType}) already processed. Skipping.", eventId, eventType);
            return;
        }

        var webhookLog = new WebhookEvent
        {
            StripeEventId = eventId,
            EventType = eventType,
            PayloadJson = payloadJson,
            Status = "Processing",
            ReceivedAtUtc = DateTime.UtcNow
        };

        await _context.WebhookEvents.AddAsync(webhookLog);
        await _context.SaveChangesAsync();

        try
        {
            var dataObj = root.GetProperty("data").GetProperty("object");

            switch (eventType)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(dataObj);
                    break;

                case "invoice.paid":
                    await HandleInvoicePaidAsync(dataObj);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailedAsync(dataObj);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(dataObj);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(dataObj);
                    break;

                default:
                    _logger.LogInformation("Unhandled Stripe Webhook Event: {EventType}", eventType);
                    break;
            }

            webhookLog.Status = "Processed";
            webhookLog.ProcessedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook {EventId} ({EventType})", eventId, eventType);
            webhookLog.Status = "Failed";
            webhookLog.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync();
            throw;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(JsonElement sessionObj)
    {
        var customerId = sessionObj.TryGetProperty("customer", out var custProp) ? custProp.GetString() : null;
        var subscriptionId = sessionObj.TryGetProperty("subscription", out var subProp) ? subProp.GetString() : null;

        int? internalUserId = null;
        int? internalPlanId = null;

        if (sessionObj.TryGetProperty("metadata", out var meta))
        {
            if (meta.TryGetProperty("InternalUserId", out var uidProp) && int.TryParse(uidProp.GetString(), out var uid))
                internalUserId = uid;

            if (meta.TryGetProperty("InternalPlanId", out var pidProp) && int.TryParse(pidProp.GetString(), out var pid))
                internalPlanId = pid;
        }

        if (internalUserId == null && !string.IsNullOrEmpty(customerId))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
            internalUserId = user?.Id;
        }

        if (internalUserId == null || internalPlanId == null)
        {
            _logger.LogWarning("checkout.session.completed missing user/plan metadata.");
            return;
        }

        var plan = await _context.SubscriptionPlans.FindAsync(internalPlanId.Value);
        if (plan == null) return;

        var existingSub = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == internalUserId.Value && s.PlanId == internalPlanId.Value);

        var now = DateTime.UtcNow;
        var periodEnd = plan.BillingInterval == BillingInterval.Yearly ? now.AddYears(1) : now.AddMonths(1);

        if (existingSub != null)
        {
            existingSub.StripeSubscriptionId = subscriptionId ?? existingSub.StripeSubscriptionId;
            existingSub.Status = SubscriptionStatus.Active;
            existingSub.CurrentPeriodStartUtc = now;
            existingSub.CurrentPeriodEndUtc = periodEnd;
            existingSub.CancelAtPeriodEnd = false;
        }
        else
        {
            var newSub = new Subscription
            {
                UserId = internalUserId.Value,
                PlanId = internalPlanId.Value,
                StripeSubscriptionId = subscriptionId ?? $"sub_sim_{Guid.NewGuid():N}",
                Status = SubscriptionStatus.Active,
                CurrentPeriodStartUtc = now,
                CurrentPeriodEndUtc = periodEnd,
                CancelAtPeriodEnd = false,
                CreatedAtUtc = now
            };
            await _context.Subscriptions.AddAsync(newSub);
        }

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(internalUserId, "WEBHOOK_CHECKOUT_COMPLETED", "Subscription", subscriptionId ?? "");
    }

    private async Task HandleInvoicePaidAsync(JsonElement invoiceObj)
    {
        var stripeInvoiceId = invoiceObj.GetProperty("id").GetString() ?? "";
        var customerId = invoiceObj.TryGetProperty("customer", out var cp) ? cp.GetString() : null;
        var subscriptionId = invoiceObj.TryGetProperty("subscription", out var sp) ? sp.GetString() : null;
        var paymentIntentId = invoiceObj.TryGetProperty("payment_intent", out var pip) ? pip.GetString() : null;
        var amountPaid = invoiceObj.TryGetProperty("amount_paid", out var app) ? app.GetInt64() / 100.0m : 0m;
        var currency = invoiceObj.TryGetProperty("currency", out var curp) ? curp.GetString() ?? "usd" : "usd";

        // Find associated user and subscription
        var user = await _context.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
        if (user == null && !string.IsNullOrEmpty(subscriptionId))
        {
            var sub = await _context.Subscriptions.Include(s => s.User).FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);
            user = sub?.User;
        }

        if (user == null)
        {
            _logger.LogWarning("invoice.paid: User not found for customer {CustomerId}", customerId);
            return;
        }

        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId || s.UserId == user.Id);

        // 1. Generate unique invoice number
        var invoiceCount = await _context.Invoices.CountAsync() + 1;
        var invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMM}-{invoiceCount:D4}";

        var invoice = new Invoice
        {
            UserId = user.Id,
            SubscriptionId = subscription?.Id,
            InvoiceNumber = invoiceNumber,
            StripeInvoiceId = stripeInvoiceId,
            BillingPeriod = $"{DateTime.UtcNow:MMM yyyy}",
            InvoiceDateUtc = DateTime.UtcNow,
            DueDateUtc = DateTime.UtcNow,
            Subtotal = amountPaid,
            Tax = 0m,
            Total = amountPaid,
            Currency = currency,
            Status = InvoiceStatus.Paid,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _context.Invoices.AddAsync(invoice);
        await _context.SaveChangesAsync();

        // 2. Record Payment
        var payment = new Payment
        {
            UserId = user.Id,
            SubscriptionId = subscription?.Id,
            InvoiceId = invoice.Id,
            StripePaymentIntentId = paymentIntentId ?? $"pi_sim_{Guid.NewGuid():N}",
            Amount = amountPaid,
            Currency = currency,
            PaymentMethodSummary = "Credit Card (Stripe)",
            Status = PaymentStatus.Succeeded,
            PaymentDateUtc = DateTime.UtcNow
        };

        await _context.Payments.AddAsync(payment);

        // 3. Update subscription status and renewal date if attached
        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.CurrentPeriodStartUtc = DateTime.UtcNow;
            var planInterval = subscription.Plan?.BillingInterval ?? BillingInterval.Monthly;
            subscription.CurrentPeriodEndUtc = planInterval == BillingInterval.Yearly
                ? DateTime.UtcNow.AddYears(1)
                : DateTime.UtcNow.AddMonths(1);
        }

        await _context.SaveChangesAsync();

        // 4. Send email notification
        await _emailService.SendPaymentSuccessNotificationAsync(
            user.Email,
            $"{user.FirstName} {user.LastName}",
            invoice.InvoiceNumber,
            amountPaid,
            currency);

        await _auditLogService.LogAsync(user.Id, "INVOICE_PAID", "Invoice", invoice.Id.ToString(), new { invoice.InvoiceNumber, Amount = amountPaid });
    }

    private async Task HandleInvoicePaymentFailedAsync(JsonElement invoiceObj)
    {
        var customerId = invoiceObj.TryGetProperty("customer", out var cp) ? cp.GetString() : null;
        var subscriptionId = invoiceObj.TryGetProperty("subscription", out var sp) ? sp.GetString() : null;
        var amountDue = invoiceObj.TryGetProperty("amount_due", out var adp) ? adp.GetInt64() / 100.0m : 0m;
        var currency = invoiceObj.TryGetProperty("currency", out var curp) ? curp.GetString() ?? "usd" : "usd";

        var user = await _context.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == customerId);
        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);

        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.PastDue;
        }

        var payment = new Payment
        {
            UserId = user?.Id ?? subscription?.UserId ?? 0,
            SubscriptionId = subscription?.Id,
            Amount = amountDue,
            Currency = currency,
            Status = PaymentStatus.Failed,
            FailureMessage = "Card was declined or insufficient funds.",
            PaymentDateUtc = DateTime.UtcNow
        };

        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();

        if (user != null)
        {
            await _emailService.SendPaymentFailedNotificationAsync(
                user.Email,
                $"{user.FirstName} {user.LastName}",
                $"SUB-{subscription?.Id}",
                amountDue,
                currency,
                "Card declined by issuing bank.");
        }

        if (subscription != null)
        {
            // Queue payment reminder
            var reminder = new PaymentReminder
            {
                SubscriptionId = subscription.Id,
                ScheduledForUtc = DateTime.UtcNow,
                ReminderType = ReminderType.PaymentFailed,
                Status = PaymentReminderStatus.Pending,
                RetryCount = 0
            };
            await _context.PaymentReminders.AddAsync(reminder);
            await _context.SaveChangesAsync();
        }
    }

    private async Task HandleSubscriptionUpdatedAsync(JsonElement subObj)
    {
        var subscriptionId = subObj.GetProperty("id").GetString() ?? "";
        var statusStr = subObj.TryGetProperty("status", out var sp) ? sp.GetString() : "";
        var cancelAtPeriodEnd = subObj.TryGetProperty("cancel_at_period_end", out var cap) && cap.GetBoolean();

        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);
        if (subscription == null) return;

        subscription.CancelAtPeriodEnd = cancelAtPeriodEnd;
        if (statusStr == "active") subscription.Status = SubscriptionStatus.Active;
        else if (statusStr == "past_due") subscription.Status = SubscriptionStatus.PastDue;
        else if (statusStr == "canceled")
        {
            subscription.Status = SubscriptionStatus.Canceled;
            subscription.CanceledAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(subscription.UserId, "SUBSCRIPTION_UPDATED", "Subscription", subscription.Id.ToString(), new { Status = statusStr, CancelAtPeriodEnd = cancelAtPeriodEnd });
    }

    private async Task HandleSubscriptionDeletedAsync(JsonElement subObj)
    {
        var subscriptionId = subObj.GetProperty("id").GetString() ?? "";
        var subscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);
        if (subscription == null) return;

        subscription.Status = SubscriptionStatus.Canceled;
        subscription.CanceledAtUtc = DateTime.UtcNow;
        subscription.CancelAtPeriodEnd = false;

        await _context.SaveChangesAsync();
        await _auditLogService.LogAsync(subscription.UserId, "SUBSCRIPTION_DELETED", "Subscription", subscription.Id.ToString());
    }
}
