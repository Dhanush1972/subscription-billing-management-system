using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Application.Services;
using SubscriptionBilling.Application.Validators;

namespace SubscriptionBilling.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IStripeWebhookService, StripeWebhookService>();

        return services;
    }
}
