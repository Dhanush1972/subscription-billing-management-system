using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Payments;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Exceptions;

namespace SubscriptionBilling.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IApplicationDbContext _context;

    public PaymentService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<PaymentDto>> GetCustomerPaymentsAsync(int userId, PaymentFilterRequest filter)
    {
        var query = _context.Payments
            .Include(p => p.User)
            .Include(p => p.Invoice)
            .Where(p => p.UserId == userId)
            .AsNoTracking();

        if (filter.Status.HasValue)
        {
            query = query.Where(p => p.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(p => p.PaymentDateUtc >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(p => p.PaymentDateUtc <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.PaymentDateUtc)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return new PagedResult<PaymentDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<PagedResult<PaymentDto>> GetAllPaymentsForAdminAsync(PaymentFilterRequest filter)
    {
        var query = _context.Payments
            .Include(p => p.User)
            .Include(p => p.Invoice)
            .AsNoTracking();

        if (filter.Status.HasValue)
        {
            query = query.Where(p => p.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(p => p.PaymentDateUtc >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(p => p.PaymentDateUtc <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(p => p.User.Email.ToLower().Contains(search) ||
                                     (p.Invoice != null && p.Invoice.InvoiceNumber.ToLower().Contains(search)) ||
                                     (p.StripePaymentIntentId != null && p.StripePaymentIntentId.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.PaymentDateUtc)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return new PagedResult<PaymentDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<PaymentDto> GetPaymentByIdAsync(int paymentId, int? userId = null)
    {
        var payment = await _context.Payments
            .Include(p => p.User)
            .Include(p => p.Invoice)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null)
        {
            throw new NotFoundException(nameof(Payment), paymentId);
        }

        if (userId.HasValue && payment.UserId != userId.Value)
        {
            throw new NotFoundException(nameof(Payment), paymentId);
        }

        return MapToDto(payment);
    }

    private static PaymentDto MapToDto(Payment p) => new(
        p.Id,
        p.UserId,
        p.User?.Email ?? "",
        p.SubscriptionId,
        p.InvoiceId,
        p.Invoice?.InvoiceNumber,
        p.StripePaymentIntentId,
        p.Amount,
        p.Currency,
        p.PaymentMethodSummary ?? "Credit Card",
        p.Status,
        p.FailureMessage,
        p.PaymentDateUtc
    );
}
