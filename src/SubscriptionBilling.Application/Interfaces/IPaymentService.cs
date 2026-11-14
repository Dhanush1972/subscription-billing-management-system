using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Payments;

namespace SubscriptionBilling.Application.Interfaces;

public interface IPaymentService
{
    Task<PagedResult<PaymentDto>> GetCustomerPaymentsAsync(int userId, PaymentFilterRequest filter);
    Task<PagedResult<PaymentDto>> GetAllPaymentsForAdminAsync(PaymentFilterRequest filter);
    Task<PaymentDto> GetPaymentByIdAsync(int paymentId, int? userId = null);
}
