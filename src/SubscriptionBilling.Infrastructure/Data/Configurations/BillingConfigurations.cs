using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionBilling.Domain.Entities;

namespace SubscriptionBilling.Infrastructure.Data.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.Name).IsRequired().HasMaxLength(100);
        builder.Property(sp => sp.Description).HasMaxLength(500);
        builder.Property(sp => sp.Price).HasPrecision(18, 2);
        builder.Property(sp => sp.Currency).IsRequired().HasMaxLength(5);
        builder.Property(sp => sp.BillingInterval).HasConversion<string>().HasMaxLength(20);
        builder.Property(sp => sp.StripeProductId).HasMaxLength(100);
        builder.Property(sp => sp.StripePriceId).HasMaxLength(100);
        builder.Property(sp => sp.IsPopular).HasDefaultValue(false);
        builder.HasIndex(sp => sp.StripePriceId);

        builder.HasMany(sp => sp.Prices)
            .WithOne(p => p.SubscriptionPlan)
            .HasForeignKey(p => p.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sp => sp.PlanFeatures)
            .WithOne(f => f.SubscriptionPlan)
            .HasForeignKey(f => f.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SubscriptionPlanPriceConfiguration : IEntityTypeConfiguration<SubscriptionPlanPrice>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanPrice> builder)
    {
        builder.ToTable("SubscriptionPlanPrices");
        builder.HasKey(spp => spp.Id);
        builder.Property(spp => spp.Amount).HasPrecision(18, 2);
        builder.Property(spp => spp.Currency).IsRequired().HasMaxLength(5);
        builder.Property(spp => spp.BillingInterval).HasConversion<string>().HasMaxLength(20);
        builder.Property(spp => spp.StripePriceId).HasMaxLength(100);

        // Unique composite index to prevent duplicate pricing for the same plan, interval, and currency
        builder.HasIndex(spp => new { spp.SubscriptionPlanId, spp.BillingInterval, spp.Currency })
            .IsUnique();

        builder.HasOne(spp => spp.SubscriptionPlan)
            .WithMany(sp => sp.Prices)
            .HasForeignKey(spp => spp.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubscriptionPlanFeatureConfiguration : IEntityTypeConfiguration<SubscriptionPlanFeature>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlanFeature> builder)
    {
        builder.ToTable("SubscriptionPlanFeatures");
        builder.HasKey(spf => spf.Id);
        builder.Property(spf => spf.FeatureName).IsRequired().HasMaxLength(100);
        builder.Property(spf => spf.FeatureValue).IsRequired().HasMaxLength(255);
        builder.HasIndex(spf => spf.SubscriptionPlanId);

        builder.HasOne(spf => spf.SubscriptionPlan)
            .WithMany(sp => sp.PlanFeatures)
            .HasForeignKey(spf => spf.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(100);
        builder.HasIndex(s => s.StripeSubscriptionId).IsUnique().HasFilter("`StripeSubscriptionId` IS NOT NULL");
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Plan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SubscriptionPlanPrice)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(s => s.SubscriptionPlanPriceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(5);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.StripePaymentIntentId).HasMaxLength(100);
        builder.HasIndex(p => p.StripePaymentIntentId).IsUnique().HasFilter("`StripePaymentIntentId` IS NOT NULL");
        builder.Property(p => p.StripeChargeId).HasMaxLength(100);
        builder.Property(p => p.PaymentMethodSummary).HasMaxLength(100);

        builder.HasOne(p => p.User)
            .WithMany(u => u.Payments)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.Property(i => i.StripeInvoiceId).HasMaxLength(100);
        builder.HasIndex(i => i.StripeInvoiceId).IsUnique().HasFilter("`StripeInvoiceId` IS NOT NULL");
        builder.Property(i => i.BillingPeriod).HasMaxLength(100);
        builder.Property(i => i.Subtotal).HasPrecision(18, 2);
        builder.Property(i => i.Tax).HasPrecision(18, 2);
        builder.Property(i => i.Total).HasPrecision(18, 2);
        builder.Property(i => i.Currency).IsRequired().HasMaxLength(5);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(i => i.PdfFilePath).HasMaxLength(500);

        builder.HasOne(i => i.User)
            .WithMany(u => u.Invoices)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Subscription)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PaymentReminderConfiguration : IEntityTypeConfiguration<PaymentReminder>
{
    public void Configure(EntityTypeBuilder<PaymentReminder> builder)
    {
        builder.ToTable("PaymentReminders");
        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.ReminderType).HasConversion<string>().HasMaxLength(30);
        builder.Property(pr => pr.Status).HasConversion<string>().HasMaxLength(30);

        builder.HasOne(pr => pr.Subscription)
            .WithMany(s => s.PaymentReminders)
            .HasForeignKey(pr => pr.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("WebhookEvents");
        builder.HasKey(we => we.Id);
        builder.Property(we => we.StripeEventId).IsRequired().HasMaxLength(100);
        builder.HasIndex(we => we.StripeEventId).IsUnique();
        builder.Property(we => we.EventType).IsRequired().HasMaxLength(100);
        builder.Property(we => we.Status).IsRequired().HasMaxLength(30);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(al => al.Id);
        builder.Property(al => al.Action).IsRequired().HasMaxLength(100);
        builder.Property(al => al.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(al => al.EntityId).HasMaxLength(100);
        builder.Property(al => al.IpAddress).HasMaxLength(50);

        builder.HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
