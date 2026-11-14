using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Payments;
using SubscriptionBilling.Application.Interfaces;

namespace SubscriptionBilling.API.Controllers;

[Authorize]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPayments([FromQuery] PaymentFilterRequest filter)
    {
        var result = await _paymentService.GetCustomerPaymentsAsync(CurrentUserId, filter);
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPaymentsForAdmin([FromQuery] PaymentFilterRequest filter)
    {
        var result = await _paymentService.GetAllPaymentsForAdminAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userIdFilter = User.IsInRole("Admin") ? (int?)null : CurrentUserId;
        var payment = await _paymentService.GetPaymentByIdAsync(id, userIdFilter);
        return Ok(payment);
    }
}
