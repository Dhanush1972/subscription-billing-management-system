using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Application.DTOs.Payments;

public record PaymentDto(
    int Id,
    int UserId,
    string UserEmail,
    int? SubscriptionId,
    int? InvoiceId,
    string? InvoiceNumber,
    string? StripePaymentIntentId,
    decimal Amount,
    string Currency,
    string? PaymentMethodSummary,
    PaymentStatus Status,
    string? FailureMessage,
    DateTime PaymentDateUtc
);

public record PaymentFilterRequest(
    int PageNumber = 1,
    int PageSize = 10,
    PaymentStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Search = null
);
