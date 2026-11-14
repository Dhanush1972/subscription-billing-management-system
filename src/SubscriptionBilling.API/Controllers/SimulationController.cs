using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Application.Services;

namespace SubscriptionBilling.API.Controllers;

[AllowAnonymous]
public class SimulationController : BaseApiController
{
    private readonly IStripeWebhookService _webhookService;
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public SimulationController(
        IStripeWebhookService webhookService,
        IApplicationDbContext context,
        IConfiguration configuration)
    {
        _webhookService = webhookService;
        _context = context;
        _configuration = configuration;
    }

    public record SimulatePaymentRequest(
        int UserId,
        int PlanId
    );

    [HttpPost("simulate-payment")]
    public async Task<IActionResult> SimulateSuccessfulPayment([FromBody] SimulatePaymentRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null) return NotFound(new { Message = "User not found." });

        var plan = await _context.SubscriptionPlans.FindAsync(request.PlanId);
        if (plan == null) return NotFound(new { Message = "Plan not found." });

        var eventId = $"evt_sim_{Guid.NewGuid():N}";
        var subscriptionId = $"sub_sim_{Guid.NewGuid():N}";
        var invoiceId = $"in_sim_{Guid.NewGuid():N}";
        var paymentIntentId = $"pi_sim_{Guid.NewGuid():N}";

        // 1. First simulate checkout.session.completed
        var checkoutPayload = new
        {
            id = eventId + "_cs",
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    customer = user.StripeCustomerId ?? $"cus_sim_{user.Id}",
                    subscription = subscriptionId,
                    metadata = new
                    {
                        InternalUserId = user.Id.ToString(),
                        InternalPlanId = plan.Id.ToString()
                    }
                }
            }
        };

        var secret = _configuration["Stripe:WebhookSecret"] ?? "whsec_placeholder";
        await _webhookService.ProcessWebhookEventAsync(JsonSerializer.Serialize(checkoutPayload), "simulated-signature", secret);

        // 2. Next simulate invoice.paid
        var invoicePayload = new
        {
            id = eventId + "_inv",
            type = "invoice.paid",
            data = new
            {
                @object = new
                {
                    id = invoiceId,
                    customer = user.StripeCustomerId ?? $"cus_sim_{user.Id}",
                    subscription = subscriptionId,
                    payment_intent = paymentIntentId,
                    amount_paid = (long)(plan.Price * 100),
                    currency = plan.Currency
                }
            }
        };

        await _webhookService.ProcessWebhookEventAsync(JsonSerializer.Serialize(invoicePayload), "simulated-signature", secret);

        return Ok(new
        {
            Success = true,
            Message = "Payment simulated successfully through real webhook processing pipeline.",
            SubscriptionId = subscriptionId,
            InvoiceId = invoiceId,
            Amount = plan.Price
        });
    }
}
