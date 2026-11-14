using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Infrastructure.Data;

namespace SubscriptionBilling.Tests;

public static class TestDbContextFactory
{
    public static SubscriptionBillingDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<SubscriptionBillingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new SubscriptionBillingDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
