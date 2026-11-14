using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Invoices;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Exceptions;

namespace SubscriptionBilling.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IApplicationDbContext _context;
    private readonly IPdfInvoiceGenerator _pdfGenerator;

    public InvoiceService(IApplicationDbContext context, IPdfInvoiceGenerator pdfGenerator)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<PagedResult<InvoiceDto>> GetCustomerInvoicesAsync(int userId, InvoiceFilterRequest filter)
    {
        var query = _context.Invoices
            .Include(i => i.User)
            .Include(i => i.Subscription)
                .ThenInclude(s => s!.Plan)
            .Where(i => i.UserId == userId)
            .AsNoTracking();

        if (filter.Status.HasValue)
        {
            query = query.Where(i => i.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDateUtc >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDateUtc <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.InvoiceDateUtc)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(i => MapToDto(i))
            .ToListAsync();

        return new PagedResult<InvoiceDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<PagedResult<InvoiceDto>> GetAllInvoicesForAdminAsync(InvoiceFilterRequest filter)
    {
        var query = _context.Invoices
            .Include(i => i.User)
            .Include(i => i.Subscription)
                .ThenInclude(s => s!.Plan)
            .AsNoTracking();

        if (filter.Status.HasValue)
        {
            query = query.Where(i => i.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDateUtc >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDateUtc <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(i => i.InvoiceNumber.ToLower().Contains(search) ||
                                     i.User.Email.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.InvoiceDateUtc)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(i => MapToDto(i))
            .ToListAsync();

        return new PagedResult<InvoiceDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(int invoiceId, int? userId = null)
    {
        var invoice = await _context.Invoices
            .Include(i => i.User)
            .Include(i => i.Subscription)
                .ThenInclude(s => s!.Plan)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            throw new NotFoundException(nameof(Invoice), invoiceId);
        }

        if (userId.HasValue && invoice.UserId != userId.Value)
        {
            throw new NotFoundException(nameof(Invoice), invoiceId);
        }

        return MapToDto(invoice);
    }

    public async Task<(byte[] FileBytes, string ContentType, string FileName)> DownloadInvoicePdfAsync(int invoiceId, int? userId = null)
    {
        var invoice = await _context.Invoices
            .Include(i => i.User)
            .Include(i => i.Subscription)
                .ThenInclude(s => s!.Plan)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            throw new NotFoundException(nameof(Invoice), invoiceId);
        }

        if (userId.HasValue && invoice.UserId != userId.Value)
        {
            throw new NotFoundException(nameof(Invoice), invoiceId);
        }

        var pdfBytes = _pdfGenerator.GenerateInvoicePdf(invoice, invoice.User, invoice.Subscription?.Plan);
        var fileName = $"Invoice-{invoice.InvoiceNumber}.pdf";

        return (pdfBytes, "application/pdf", fileName);
    }

    private static InvoiceDto MapToDto(Invoice i) => new(
        i.Id,
        i.UserId,
        i.User?.Email ?? "",
        i.SubscriptionId,
        i.Subscription?.Plan?.Name ?? "Subscription",
        i.InvoiceNumber,
        i.StripeInvoiceId,
        i.BillingPeriod,
        i.InvoiceDateUtc,
        i.DueDateUtc,
        i.Subtotal,
        i.Tax,
        i.Total,
        i.Currency,
        i.Status,
        true,
        i.CreatedAtUtc
    );
}
