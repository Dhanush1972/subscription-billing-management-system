using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Subscriptions;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.API.Controllers;

[Authorize]
public class SubscriptionsController : BaseApiController
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubscription()
    {
        var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(CurrentUserId);
        return Ok(subscription);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<SubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllSubscriptions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] SubscriptionStatus? status = null)
    {
        var result = await _subscriptionService.GetAllSubscriptionsAsync(pageNumber, pageSize, status);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
        if (!User.IsInRole("Admin") && subscription.UserId != CurrentUserId)
        {
            return Forbid();
        }

        return Ok(subscription);
    }

    [HttpPost("checkout-session")]
    [ProducesResponseType(typeof(CheckoutSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
    {
        var response = await _subscriptionService.CreateCheckoutSessionAsync(CurrentUserId, request);
        return Ok(response);
    }

    [HttpPost("upgrade")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpgradeOrDowngrade([FromBody] UpgradeSubscriptionRequest request)
    {
        var updated = await _subscriptionService.UpgradeOrDowngradeSubscriptionAsync(CurrentUserId, request);
        return Ok(updated);
    }

    [HttpPost("cancel")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel([FromBody] CancelSubscriptionRequest request)
    {
        var canceled = await _subscriptionService.CancelSubscriptionAsync(CurrentUserId, request);
        return Ok(canceled);
    }

    [HttpPost("reactivate")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reactivate()
    {
        var reactivated = await _subscriptionService.ReactivateSubscriptionAsync(CurrentUserId);
        return Ok(reactivated);
    }
}
