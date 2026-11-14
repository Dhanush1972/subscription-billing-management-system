using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Enums;
using SubscriptionBilling.Infrastructure.Data;

namespace SubscriptionBilling.Infrastructure.Services;

public class PaymentReminderProcessor : IPaymentReminderProcessor
{
    private readonly SubscriptionBillingDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<PaymentReminderProcessor> _logger;

    public PaymentReminderProcessor(
        SubscriptionBillingDbContext context,
        IEmailService emailService,
        ILogger<PaymentReminderProcessor> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessUpcomingRenewalsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(70);
        var windowEnd = now.AddHours(78);

        _logger.LogInformation("Scanning subscriptions renewing between {Start} and {End}...", windowStart, windowEnd);

        var renewingSubscriptions = await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        !s.CancelAtPeriodEnd &&
                        s.CurrentPeriodEndUtc >= windowStart &&
                        s.CurrentPeriodEndUtc <= windowEnd)
            .ToListAsync(cancellationToken);

        foreach (var sub in renewingSubscriptions)
        {
            // Prevent duplicate reminder
            var alreadyReminded = await _context.PaymentReminders
                .AnyAsync(r => r.SubscriptionId == sub.Id &&
                               r.ReminderType == ReminderType.UpcomingRenewal &&
                               r.Status == PaymentReminderStatus.Sent &&
                               r.ScheduledForUtc >= now.AddDays(-5), cancellationToken);

            if (alreadyReminded)
            {
                continue;
            }

            var reminder = new PaymentReminder
            {
                SubscriptionId = sub.Id,
                ScheduledForUtc = now,
                ReminderType = ReminderType.UpcomingRenewal,
                Status = PaymentReminderStatus.Pending,
                RetryCount = 0
            };

            await _context.PaymentReminders.AddAsync(reminder, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await _emailService.SendRenewalReminderAsync(
                    sub.User.Email,
                    $"{sub.User.FirstName} {sub.User.LastName}",
                    sub.Plan.Name,
                    sub.CurrentPeriodEndUtc,
                    sub.Plan.Price,
                    sub.Plan.Currency);

                reminder.Status = PaymentReminderStatus.Sent;
                reminder.SentAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send renewal reminder for subscription {SubId}", sub.Id);
                reminder.Status = PaymentReminderStatus.Failed;
                reminder.ErrorMessage = ex.Message;
                reminder.RetryCount++;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ProcessFailedPaymentRetriesAsync(CancellationToken cancellationToken = default)
    {
        var pendingReminders = await _context.PaymentReminders
            .Include(r => r.Subscription)
                .ThenInclude(s => s.User)
            .Include(r => r.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(r => r.Status == PaymentReminderStatus.Pending ||
                       (r.Status == PaymentReminderStatus.Failed && r.RetryCount < 3))
            .ToListAsync(cancellationToken);

        foreach (var reminder in pendingReminders)
        {
            try
            {
                if (reminder.ReminderType == ReminderType.PaymentFailed)
                {
                    await _emailService.SendPaymentFailedNotificationAsync(
                        reminder.Subscription.User.Email,
                        $"{reminder.Subscription.User.FirstName} {reminder.Subscription.User.LastName}",
                        $"SUB-{reminder.SubscriptionId}",
                        reminder.Subscription.Plan.Price,
                        reminder.Subscription.Plan.Currency,
                        reminder.ErrorMessage);
                }

                reminder.Status = PaymentReminderStatus.Sent;
                reminder.SentAtUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                reminder.RetryCount++;
                reminder.Status = reminder.RetryCount >= 3 ? PaymentReminderStatus.Failed : PaymentReminderStatus.Pending;
                reminder.ErrorMessage = ex.Message;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
