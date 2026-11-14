using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Domain.Enums;
using SubscriptionBilling.Infrastructure.Data;
using Xunit;

namespace SubscriptionBilling.Tests;

public class SubscriptionPlanSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesSamplePlansWithPricesAndFeatures_AndIsIdempotent()
    {
        // Arrange
        using var context = TestDbContextFactory.Create(nameof(SeedAsync_CreatesSamplePlansWithPricesAndFeatures_AndIsIdempotent));

        // Act 1: Initial seed
        await DatabaseSeeder.SeedAsync(context);

        // Assert 1: Verify all 3 plans exist
        var plans = await context.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.PlanFeatures)
            .ToListAsync();

        plans.Should().HaveCount(3);
        plans.Select(p => p.Name).Should().Contain(new[] { "Basic", "Professional", "Enterprise" });

        // Verify Professional is Popular
        var pro = plans.First(p => p.Name == "Professional");
        pro.IsPopular.Should().BeTrue();

        // Verify Basic is not popular
        var basic = plans.First(p => p.Name == "Basic");
        basic.IsPopular.Should().BeFalse();

        // Verify Prices for Basic
        basic.Prices.Should().HaveCount(2);
        var basicMonthly = basic.Prices.First(pr => pr.BillingInterval == BillingInterval.Monthly);
        basicMonthly.Amount.Should().Be(499.00m);
        basicMonthly.Currency.Should().Be("INR");

        var basicYearly = basic.Prices.First(pr => pr.BillingInterval == BillingInterval.Yearly);
        basicYearly.Amount.Should().Be(4990.00m);
        basicYearly.Currency.Should().Be("INR");

        // Verify Prices for Professional
        pro.Prices.Should().HaveCount(2);
        pro.Prices.First(pr => pr.BillingInterval == BillingInterval.Monthly).Amount.Should().Be(999.00m);
        pro.Prices.First(pr => pr.BillingInterval == BillingInterval.Yearly).Amount.Should().Be(9990.00m);

        // Verify Prices for Enterprise
        var enterprise = plans.First(p => p.Name == "Enterprise");
        enterprise.Prices.Should().HaveCount(2);
        enterprise.Prices.First(pr => pr.BillingInterval == BillingInterval.Monthly).Amount.Should().Be(2499.00m);
        enterprise.Prices.First(pr => pr.BillingInterval == BillingInterval.Yearly).Amount.Should().Be(24990.00m);

        // Verify Features for Basic
        basic.PlanFeatures.Should().Contain(f => f.FeatureName == "MaxUsers" && f.FeatureValue == "1");
        basic.PlanFeatures.Should().Contain(f => f.FeatureName == "MaxProjects" && f.FeatureValue == "5");
        basic.PlanFeatures.Should().Contain(f => f.FeatureName == "Storage" && f.FeatureValue == "5 GB");

        // Verify Features for Professional
        pro.PlanFeatures.Should().Contain(f => f.FeatureName == "MaxUsers" && f.FeatureValue == "5");
        pro.PlanFeatures.Should().Contain(f => f.FeatureName == "MaxProjects" && f.FeatureValue == "25");
        pro.PlanFeatures.Should().Contain(f => f.FeatureName == "Storage" && f.FeatureValue == "50 GB");

        // Verify Features for Enterprise
        enterprise.PlanFeatures.Should().Contain(f => f.FeatureName == "MaxUsers" && f.FeatureValue == "Unlimited");
        enterprise.PlanFeatures.Should().Contain(f => f.FeatureName == "MaxProjects" && f.FeatureValue == "Unlimited");
        enterprise.PlanFeatures.Should().Contain(f => f.FeatureName == "Storage" && f.FeatureValue == "500 GB");

        // Act 2: Run seeding a second time to test idempotency
        await DatabaseSeeder.SeedAsync(context);

        // Assert 2: No duplicates created
        (await context.SubscriptionPlans.CountAsync()).Should().Be(3);
        (await context.SubscriptionPlanPrices.CountAsync()).Should().Be(6);
        (await context.Roles.CountAsync()).Should().Be(2);
    }
}
