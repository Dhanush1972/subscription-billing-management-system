using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Subscriptions;
using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Application.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionDto?> GetCurrentSubscriptionAsync(int userId);
    Task<SubscriptionDto> GetSubscriptionByIdAsync(int id);
    Task<PagedResult<SubscriptionDto>> GetAllSubscriptionsAsync(int pageNumber = 1, int pageSize = 10, SubscriptionStatus? status = null);
    Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(int userId, CreateCheckoutSessionRequest request);
    Task<SubscriptionDto> UpgradeOrDowngradeSubscriptionAsync(int userId, UpgradeSubscriptionRequest request);
    Task<SubscriptionDto> CancelSubscriptionAsync(int userId, CancelSubscriptionRequest request);
    Task<SubscriptionDto> ReactivateSubscriptionAsync(int userId);
}
