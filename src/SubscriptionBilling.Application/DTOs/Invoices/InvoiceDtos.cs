using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Application.DTOs.Invoices;

public record InvoiceDto(
    int Id,
    int UserId,
    string UserEmail,
    int? SubscriptionId,
    string? PlanName,
    string InvoiceNumber,
    string? StripeInvoiceId,
    string BillingPeriod,
    DateTime InvoiceDateUtc,
    DateTime DueDateUtc,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    string Currency,
    InvoiceStatus Status,
    bool HasPdf,
    DateTime CreatedAtUtc
);

public record InvoiceFilterRequest(
    int PageNumber = 1,
    int PageSize = 10,
    InvoiceStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null
);
