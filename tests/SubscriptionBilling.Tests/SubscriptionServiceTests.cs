using FluentAssertions;
using Moq;
using SubscriptionBilling.Application.DTOs.Subscriptions;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Application.Services;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Enums;
using SubscriptionBilling.Domain.Exceptions;
using Xunit;

namespace SubscriptionBilling.Tests;

public class SubscriptionServiceTests
{
    private readonly Mock<IStripePaymentGateway> _stripeMock;
    private readonly Mock<IAuditLogService> _auditMock;

    public SubscriptionServiceTests()
    {
        _stripeMock = new Mock<IStripePaymentGateway>();
        _auditMock = new Mock<IAuditLogService>();
    }

    [Fact]
    public async Task CreateCheckoutSessionAsync_WithValidPlan_ReturnsSessionResponse()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(CreateCheckoutSessionAsync_WithValidPlan_ReturnsSessionResponse));
        var user = new User { Id = 1, Email = "sub@test.com", StripeCustomerId = "cus_123", FirstName = "A", LastName = "B" };
        var plan = new SubscriptionPlan { Id = 5, Name = "Pro Plan", Price = 49m, StripePriceId = "price_pro", IsActive = true };
        context.Users.Add(user);
        context.SubscriptionPlans.Add(plan);
        await context.SaveChangesAsync();

        _stripeMock.Setup(s => s.CreateCheckoutSessionAsync("cus_123", "price_pro", 1, 5, "http://ok", "http://cancel"))
            .ReturnsAsync(new CheckoutSessionResponse("cs_123", "https://checkout.stripe.com/c/pay/cs_123"));

        var service = new SubscriptionService(context, _stripeMock.Object, _auditMock.Object);

        // Act
        var result = await service.CreateCheckoutSessionAsync(1, new CreateCheckoutSessionRequest(5, "http://ok", "http://cancel"));

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be("cs_123");
        result.CheckoutUrl.Should().Contain("stripe.com");
    }

    [Fact]
    public async Task UpgradeOrDowngradeSubscriptionAsync_ToNewPlan_UpdatesDatabaseAndCallsStripe()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(UpgradeOrDowngradeSubscriptionAsync_ToNewPlan_UpdatesDatabaseAndCallsStripe));
        var plan1 = new SubscriptionPlan { Id = 1, Name = "Starter", Price = 19m, StripePriceId = "price_starter", IsActive = true };
        var plan2 = new SubscriptionPlan { Id = 2, Name = "Pro", Price = 49m, StripePriceId = "price_pro", IsActive = true };
        var user = new User { Id = 1, Email = "upgrade@test.com", FirstName = "U", LastName = "P" };
        var sub = new Subscription
        {
            Id = 10,
            UserId = 1,
            PlanId = 1,
            Plan = plan1,
            StripeSubscriptionId = "sub_stripe_10",
            Status = SubscriptionStatus.Active,
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(20)
        };

        context.SubscriptionPlans.AddRange(plan1, plan2);
        context.Users.Add(user);
        context.Subscriptions.Add(sub);
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, _stripeMock.Object, _auditMock.Object);

        // Act
        var updated = await service.UpgradeOrDowngradeSubscriptionAsync(1, new UpgradeSubscriptionRequest(2));

        // Assert
        updated.PlanId.Should().Be(2);
        _stripeMock.Verify(s => s.ModifySubscriptionPlanAsync("sub_stripe_10", "price_pro"), Times.Once);

        var subInDb = context.Subscriptions.First(s => s.Id == 10);
        subInDb.PlanId.Should().Be(2);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_Immediately_SetsStatusCanceled()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(CancelSubscriptionAsync_Immediately_SetsStatusCanceled));
        var plan = new SubscriptionPlan { Id = 1, Name = "Starter", Price = 19m, IsActive = true };
        var user = new User { Id = 1, Email = "cancel@test.com", FirstName = "C", LastName = "A" };
        var sub = new Subscription
        {
            Id = 20,
            UserId = 1,
            PlanId = 1,
            Plan = plan,
            StripeSubscriptionId = "sub_cancel_20",
            Status = SubscriptionStatus.Active,
            CurrentPeriodEndUtc = DateTime.UtcNow.AddDays(15)
        };

        context.SubscriptionPlans.Add(plan);
        context.Users.Add(user);
        context.Subscriptions.Add(sub);
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, _stripeMock.Object, _auditMock.Object);

        // Act
        var result = await service.CancelSubscriptionAsync(1, new CancelSubscriptionRequest(Immediately: true));

        // Assert
        result.Status.Should().Be(SubscriptionStatus.Canceled);
        _stripeMock.Verify(s => s.CancelSubscriptionAsync("sub_cancel_20", true), Times.Once);

        var subInDb = context.Subscriptions.First(s => s.Id == 20);
        subInDb.Status.Should().Be(SubscriptionStatus.Canceled);
    }
}
