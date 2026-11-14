using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionBilling.Application.Interfaces;

namespace SubscriptionBilling.Infrastructure.BackgroundJobs;

public class PaymentReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentReminderBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    public PaymentReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Reminder Background Service started with interval: {Interval}.", _checkInterval);

        // Initial delay to let the app fully initialize
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        using var timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IPaymentReminderProcessor>();

                _logger.LogInformation("Payment Reminder Background Service cycle starting...");
                await processor.ProcessUpcomingRenewalsAsync(stoppingToken);
                await processor.ProcessFailedPaymentRetriesAsync(stoppingToken);
                _logger.LogInformation("Payment Reminder Background Service cycle completed.");
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Unhandled exception occurred during payment reminder background processing.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Payment Reminder Background Service stopped.");
    }
}
