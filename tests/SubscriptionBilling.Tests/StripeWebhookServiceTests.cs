using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Application.Services;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Enums;
using Xunit;

namespace SubscriptionBilling.Tests;

public class StripeWebhookServiceTests
{
    private readonly Mock<IStripePaymentGateway> _stripeGatewayMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly Mock<IAuditLogService> _auditMock;
    private readonly Mock<ILogger<StripeWebhookService>> _loggerMock;

    public StripeWebhookServiceTests()
    {
        _stripeGatewayMock = new Mock<IStripePaymentGateway>();
        _emailMock = new Mock<IEmailService>();
        _auditMock = new Mock<IAuditLogService>();
        _loggerMock = new Mock<ILogger<StripeWebhookService>>();

        _stripeGatewayMock.Setup(s => s.VerifyWebhookSignatureAsync(It.IsAny<string>(), "valid_sig", It.IsAny<string>()))
            .ReturnsAsync(true);

        _stripeGatewayMock.Setup(s => s.VerifyWebhookSignatureAsync(It.IsAny<string>(), "invalid_sig", It.IsAny<string>()))
            .ReturnsAsync(false);
    }

    [Fact]
    public async Task ProcessWebhookEventAsync_InvalidSignature_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(ProcessWebhookEventAsync_InvalidSignature_ThrowsUnauthorizedAccessException));
        var service = new StripeWebhookService(context, _stripeGatewayMock.Object, _emailMock.Object, _auditMock.Object, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ProcessWebhookEventAsync("{}", "invalid_sig", "whsec_test"));
    }

    [Fact]
    public async Task ProcessWebhookEventAsync_DuplicateEvent_IsIdempotentAndIgnored()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(ProcessWebhookEventAsync_DuplicateEvent_IsIdempotentAndIgnored));
        context.WebhookEvents.Add(new WebhookEvent
        {
            StripeEventId = "evt_dup_123",
            EventType = "invoice.paid",
            Status = "Processed"
        });
        await context.SaveChangesAsync();

        var service = new StripeWebhookService(context, _stripeGatewayMock.Object, _emailMock.Object, _auditMock.Object, _loggerMock.Object);

        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_dup_123",
            type = "invoice.paid"
        });

        // Act
        await service.ProcessWebhookEventAsync(payload, "valid_sig", "whsec_test");

        // Assert - Should not create any new payments
        context.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessWebhookEventAsync_InvoicePaid_CreatesPaymentAndInvoiceAndSendsEmail()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(ProcessWebhookEventAsync_InvoicePaid_CreatesPaymentAndInvoiceAndSendsEmail));
        var user = new User { Id = 1, Email = "buyer@test.com", StripeCustomerId = "cus_buyer_1" };
        var plan = new SubscriptionPlan { Id = 1, Name = "Pro", Price = 49m, BillingInterval = BillingInterval.Monthly };
        var sub = new Subscription
        {
            Id = 5,
            UserId = 1,
            PlanId = 1,
            Plan = plan,
            StripeSubscriptionId = "sub_buyer_1",
            Status = SubscriptionStatus.Incomplete
        };

        context.Users.Add(user);
        context.SubscriptionPlans.Add(plan);
        context.Subscriptions.Add(sub);
        await context.SaveChangesAsync();

        var service = new StripeWebhookService(context, _stripeGatewayMock.Object, _emailMock.Object, _auditMock.Object, _loggerMock.Object);

        var payload = JsonSerializer.Serialize(new
        {
            id = "evt_inv_paid_999",
            type = "invoice.paid",
            data = new
            {
                @object = new
                {
                    id = "in_stripe_999",
                    customer = "cus_buyer_1",
                    subscription = "sub_buyer_1",
                    payment_intent = "pi_stripe_999",
                    amount_paid = 4900L,
                    currency = "usd"
                }
            }
        });

        // Act
        await service.ProcessWebhookEventAsync(payload, "valid_sig", "whsec_test");

        // Assert
        var invoice = context.Invoices.FirstOrDefault(i => i.StripeInvoiceId == "in_stripe_999");
        invoice.Should().NotBeNull();
        invoice!.Total.Should().Be(49.00m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);

        var payment = context.Payments.FirstOrDefault(p => p.StripePaymentIntentId == "pi_stripe_999");
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(49.00m);
        payment.Status.Should().Be(PaymentStatus.Succeeded);

        var updatedSub = context.Subscriptions.First(s => s.Id == 5);
        updatedSub.Status.Should().Be(SubscriptionStatus.Active);

        _emailMock.Verify(e => e.SendPaymentSuccessNotificationAsync("buyer@test.com", It.IsAny<string>(), invoice.InvoiceNumber, 49.00m, "usd"), Times.Once);
    }
}
