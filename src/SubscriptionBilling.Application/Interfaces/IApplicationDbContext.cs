using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Domain.Entities;

namespace SubscriptionBilling.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<SubscriptionPlanPrice> SubscriptionPlanPrices { get; }
    DbSet<SubscriptionPlanFeature> SubscriptionPlanFeatures { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<PaymentReminder> PaymentReminders { get; }
    DbSet<WebhookEvent> WebhookEvents { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
