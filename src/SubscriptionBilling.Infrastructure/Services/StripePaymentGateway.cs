using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using SubscriptionBilling.Application.DTOs.Subscriptions;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;

namespace SubscriptionBilling.Infrastructure.Services;

public class StripePaymentGateway : IStripePaymentGateway
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentGateway> _logger;
    private readonly bool _isConfigured;

    public StripePaymentGateway(IConfiguration configuration, ILogger<StripePaymentGateway> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["Stripe:SecretKey"];
        if (!string.IsNullOrWhiteSpace(apiKey) && !apiKey.StartsWith("sk_test_placeholder", StringComparison.OrdinalIgnoreCase))
        {
            StripeConfiguration.ApiKey = apiKey;
            _isConfigured = true;
            _logger.LogInformation("Stripe SDK initialized with active test/live credentials.");
        }
        else
        {
            _isConfigured = false;
            _logger.LogWarning("Stripe API key is not configured or is a placeholder. Operating in simulation-compatible mode.");
        }
    }

    public async Task<string> CreateCustomerAsync(User user)
    {
        if (!_isConfigured)
        {
            return $"cus_simulated_{Guid.NewGuid():N}";
        }

        var customerService = new CustomerService();
        var options = new CustomerCreateOptions
        {
            Email = user.Email,
            Name = $"{user.FirstName} {user.LastName}",
            Metadata = new Dictionary<string, string>
            {
                { "InternalUserId", user.Id.ToString() }
            }
        };

        var customer = await customerService.CreateAsync(options);
        return customer.Id;
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string customerId,
        string stripePriceId,
        int internalUserId,
        int internalPlanId,
        string successUrl,
        string cancelUrl)
    {
        if (!_isConfigured)
        {
            var simulatedId = $"cs_test_simulated_{Guid.NewGuid():N}";
            var simulatedUrl = $"{successUrl}?session_id={simulatedId}&simulated=true";
            return new CheckoutSessionResponse(simulatedId, simulatedUrl);
        }

        var sessionService = new SessionService();
        var options = new SessionCreateOptions
        {
            Customer = customerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = stripePriceId,
                    Quantity = 1
                }
            },
            SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "InternalUserId", internalUserId.ToString() },
                { "InternalPlanId", internalPlanId.ToString() }
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    { "InternalUserId", internalUserId.ToString() },
                    { "InternalPlanId", internalPlanId.ToString() }
                }
            }
        };

        var session = await sessionService.CreateAsync(options);
        return new CheckoutSessionResponse(session.Id, session.Url);
    }

    public async Task<(string StripeProductId, string StripePriceId)> CreateProductAndPriceAsync(SubscriptionPlan plan)
    {
        if (!_isConfigured)
        {
            return ($"prod_simulated_{Guid.NewGuid():N}", $"price_simulated_{Guid.NewGuid():N}");
        }

        var productService = new ProductService();
        var product = await productService.CreateAsync(new ProductCreateOptions
        {
            Name = plan.Name,
            Description = plan.Description,
            Metadata = new Dictionary<string, string>
            {
                { "InternalPlanId", plan.Id.ToString() }
            }
        });

        var priceService = new PriceService();
        var price = await priceService.CreateAsync(new PriceCreateOptions
        {
            Product = product.Id,
            UnitAmount = ConvertToStripeUnitAmount(plan.Price, plan.Currency),
            Currency = plan.Currency.ToLower(),
            Recurring = new PriceRecurringOptions
            {
                Interval = plan.BillingInterval == Domain.Enums.BillingInterval.Yearly ? "year" : "month"
            }
        });

        return (product.Id, price.Id);
    }

    public async Task<string> CreatePriceForExistingProductAsync(string productId, decimal price, string currency, string interval)
    {
        if (!_isConfigured)
        {
            return $"price_simulated_{Guid.NewGuid():N}";
        }

        var priceService = new PriceService();
        var newPrice = await priceService.CreateAsync(new PriceCreateOptions
        {
            Product = productId,
            UnitAmount = ConvertToStripeUnitAmount(price, currency),
            Currency = currency.ToLower(),
            Recurring = new PriceRecurringOptions
            {
                Interval = interval.ToLower()
            }
        });

        return newPrice.Id;
    }

    public async Task ModifySubscriptionPlanAsync(string stripeSubscriptionId, string newStripePriceId)
    {
        if (!_isConfigured || stripeSubscriptionId.StartsWith("sub_simulated_"))
        {
            _logger.LogInformation("Simulation mode: subscription {SubId} modified to price {PriceId}.", stripeSubscriptionId, newStripePriceId);
            return;
        }

        var subscriptionService = new Stripe.SubscriptionService();
        var stripeSubscription = await subscriptionService.GetAsync(stripeSubscriptionId);

        var options = new SubscriptionUpdateOptions
        {
            Items = new List<SubscriptionItemOptions>
            {
                new()
                {
                    Id = stripeSubscription.Items.Data[0].Id,
                    Price = newStripePriceId
                }
            },
            ProrationBehavior = "create_prorations"
        };

        await subscriptionService.UpdateAsync(stripeSubscriptionId, options);
    }

    public async Task CancelSubscriptionAsync(string stripeSubscriptionId, bool immediately = false)
    {
        if (!_isConfigured || stripeSubscriptionId.StartsWith("sub_simulated_"))
        {
            _logger.LogInformation("Simulation mode: subscription {SubId} cancelled (immediately: {Immediately}).", stripeSubscriptionId, immediately);
            return;
        }

        var subscriptionService = new Stripe.SubscriptionService();
        if (immediately)
        {
            await subscriptionService.CancelAsync(stripeSubscriptionId);
        }
        else
        {
            await subscriptionService.UpdateAsync(stripeSubscriptionId, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });
        }
    }

    public Task<bool> VerifyWebhookSignatureAsync(string payloadJson, string signatureHeader, string webhookSecret)
    {
        if (!_isConfigured || webhookSecret.StartsWith("whsec_placeholder", StringComparison.OrdinalIgnoreCase))
        {
            // In development simulation with placeholder secret, signature header 'simulated-signature' is permitted
            if (signatureHeader == "simulated-signature")
            {
                return Task.FromResult(true);
            }
        }

        try
        {
            EventUtility.ConstructEvent(payloadJson, signatureHeader, webhookSecret, throwOnApiVersionMismatch: false);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe Webhook signature validation failed.");
            return Task.FromResult(false);
        }
    }

    private static long ConvertToStripeUnitAmount(decimal amount, string currency)
    {
        var zeroDecimalCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA", "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        if (zeroDecimalCurrencies.Contains(currency))
        {
            return (long)Math.Round(amount, MidpointRounding.AwayFromZero);
        }

        return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
    }
}
