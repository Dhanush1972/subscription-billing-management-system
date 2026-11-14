using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Invoices;
using SubscriptionBilling.Application.Interfaces;

namespace SubscriptionBilling.API.Controllers;

[Authorize]
public class InvoicesController : BaseApiController
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyInvoices([FromQuery] InvoiceFilterRequest filter)
    {
        var result = await _invoiceService.GetCustomerInvoicesAsync(CurrentUserId, filter);
        return Ok(result);
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllInvoicesForAdmin([FromQuery] InvoiceFilterRequest filter)
    {
        var result = await _invoiceService.GetAllInvoicesForAdminAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userIdFilter = User.IsInRole("Admin") ? (int?)null : CurrentUserId;
        var invoice = await _invoiceService.GetInvoiceByIdAsync(id, userIdFilter);
        return Ok(invoice);
    }

    [HttpGet("{id:int}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var userIdFilter = User.IsInRole("Admin") ? (int?)null : CurrentUserId;
        var (fileBytes, contentType, fileName) = await _invoiceService.DownloadInvoicePdfAsync(id, userIdFilter);
        return File(fileBytes, contentType, fileName);
    }
}
