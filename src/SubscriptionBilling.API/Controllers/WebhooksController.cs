using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.Services;

namespace SubscriptionBilling.API.Controllers;

[AllowAnonymous]
public class WebhooksController : BaseApiController
{
    private readonly IStripeWebhookService _webhookService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IStripeWebhookService webhookService,
        IConfiguration configuration,
        ILogger<WebhooksController> logger)
    {
        _webhookService = webhookService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook()
    {
        string payloadJson;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            payloadJson = await reader.ReadToEndAsync();
        }

        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();
        var webhookSecret = _configuration["Stripe:WebhookSecret"] ?? "whsec_placeholder";

        _logger.LogInformation("Stripe webhook received. Payload size: {Length} bytes.", payloadJson.Length);

        try
        {
            await _webhookService.ProcessWebhookEventAsync(payloadJson, signatureHeader, webhookSecret);
            return Ok(new { Received = true });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Webhook unauthorized: {Message}", ex.Message);
            return BadRequest(new { Error = "Invalid signature." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook processing exception.");
            // Return 200 to avoid indefinite Stripe retry loops if an application error occurs after recording
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = "Failed to process webhook." });
        }
    }
}
