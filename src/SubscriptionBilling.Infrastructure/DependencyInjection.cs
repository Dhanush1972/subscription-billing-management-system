using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Infrastructure.BackgroundJobs;
using SubscriptionBilling.Infrastructure.Data;
using SubscriptionBilling.Infrastructure.Services;

namespace SubscriptionBilling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "server=localhost;port=3306;database=subscription_billing_db;user=root;password=root;";

        // Configure MySQL / MariaDB (MonsterASP uses MariaDB 10.10, MySQL 8.0 compatible)
        services.AddDbContext<SubscriptionBillingDbContext>(options =>
        {
            ServerVersion serverVersion;
            try
            {
                serverVersion = ServerVersion.AutoDetect(connectionString);
            }
            catch
            {
                // Fallback to MySQL 8.0.36 LTS compatibility (compatible with MySQL 8.0 and MariaDB 10.x used on MonsterASP)
                serverVersion = new MySqlServerVersion(new Version(8, 0, 36));
            }

            options.UseMySql(connectionString, serverVersion, mySqlOptions =>
            {
                mySqlOptions.MigrationsAssembly(typeof(SubscriptionBillingDbContext).Assembly.FullName);
                mySqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<SubscriptionBillingDbContext>());

        // Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IStripePaymentGateway, StripePaymentGateway>();
        services.AddSingleton<IPdfInvoiceGenerator, PdfInvoiceService>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IPaymentReminderProcessor, PaymentReminderProcessor>();

        // Background Service
        services.AddHostedService<PaymentReminderBackgroundService>();

        return services;
    }
}
