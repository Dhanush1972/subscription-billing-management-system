using SubscriptionBilling.Application.DTOs.Subscriptions;
using SubscriptionBilling.Domain.Entities;

namespace SubscriptionBilling.Application.Interfaces;

public interface IStripePaymentGateway
{
    Task<string> CreateCustomerAsync(User user);
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
        string customerId,
        string stripePriceId,
        int internalUserId,
        int internalPlanId,
        string successUrl,
        string cancelUrl);

    Task<(string StripeProductId, string StripePriceId)> CreateProductAndPriceAsync(SubscriptionPlan plan);
    Task<string> CreatePriceForExistingProductAsync(string productId, decimal price, string currency, string interval);
    Task ModifySubscriptionPlanAsync(string stripeSubscriptionId, string newStripePriceId);
    Task CancelSubscriptionAsync(string stripeSubscriptionId, bool immediately = false);
    Task<bool> VerifyWebhookSignatureAsync(string payloadJson, string signatureHeader, string webhookSecret);
}
