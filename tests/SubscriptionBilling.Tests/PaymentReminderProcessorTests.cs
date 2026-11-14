using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Enums;
using SubscriptionBilling.Infrastructure.Services;
using Xunit;

namespace SubscriptionBilling.Tests;

public class PaymentReminderProcessorTests
{
    private readonly Mock<IEmailService> _emailMock;
    private readonly Mock<ILogger<PaymentReminderProcessor>> _loggerMock;

    public PaymentReminderProcessorTests()
    {
        _emailMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<PaymentReminderProcessor>>();
    }

    [Fact]
    public async Task ProcessUpcomingRenewalsAsync_WhenRenewalDueIn72Hours_SendsEmailAndLogsReminder()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(ProcessUpcomingRenewalsAsync_WhenRenewalDueIn72Hours_SendsEmailAndLogsReminder));
        var user = new User { Id = 1, Email = "renew@test.com", FirstName = "Sam", LastName = "Alt" };
        var plan = new SubscriptionPlan { Id = 1, Name = "Pro", Price = 49m, Currency = "usd" };
        var sub = new Subscription
        {
            Id = 1,
            UserId = 1,
            User = user,
            PlanId = 1,
            Plan = plan,
            Status = SubscriptionStatus.Active,
            CurrentPeriodEndUtc = DateTime.UtcNow.AddHours(72),
            CancelAtPeriodEnd = false
        };

        context.Users.Add(user);
        context.SubscriptionPlans.Add(plan);
        context.Subscriptions.Add(sub);
        await context.SaveChangesAsync();

        var processor = new PaymentReminderProcessor(context, _emailMock.Object, _loggerMock.Object);

        // Act
        await processor.ProcessUpcomingRenewalsAsync();

        // Assert
        _emailMock.Verify(e => e.SendRenewalReminderAsync("renew@test.com", "Sam Alt", "Pro", sub.CurrentPeriodEndUtc, 49m, "usd"), Times.Once);

        var reminder = context.PaymentReminders.FirstOrDefault(r => r.SubscriptionId == 1);
        reminder.Should().NotBeNull();
        reminder!.Status.Should().Be(PaymentReminderStatus.Sent);
    }

    [Fact]
    public async Task ProcessUpcomingRenewalsAsync_WhenAlreadyReminded_DoesNotSendDuplicateEmail()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(ProcessUpcomingRenewalsAsync_WhenAlreadyReminded_DoesNotSendDuplicateEmail));
        var user = new User { Id = 1, Email = "noduplicate@test.com", FirstName = "Tim", LastName = "Cook" };
        var plan = new SubscriptionPlan { Id = 1, Name = "Enterprise", Price = 499m, Currency = "usd" };
        var sub = new Subscription
        {
            Id = 2,
            UserId = 1,
            User = user,
            PlanId = 1,
            Plan = plan,
            Status = SubscriptionStatus.Active,
            CurrentPeriodEndUtc = DateTime.UtcNow.AddHours(72),
            CancelAtPeriodEnd = false
        };

        // Existing reminder recorded 1 day ago
        var existingReminder = new PaymentReminder
        {
            SubscriptionId = 2,
            ReminderType = ReminderType.UpcomingRenewal,
            Status = PaymentReminderStatus.Sent,
            ScheduledForUtc = DateTime.UtcNow.AddDays(-1),
            SentAtUtc = DateTime.UtcNow.AddDays(-1)
        };

        context.Users.Add(user);
        context.SubscriptionPlans.Add(plan);
        context.Subscriptions.Add(sub);
        context.PaymentReminders.Add(existingReminder);
        await context.SaveChangesAsync();

        var processor = new PaymentReminderProcessor(context, _emailMock.Object, _loggerMock.Object);

        // Act
        await processor.ProcessUpcomingRenewalsAsync();

        // Assert
        _emailMock.Verify(e => e.SendRenewalReminderAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<decimal>(), It.IsAny<string>()), Times.Never);
    }
}
