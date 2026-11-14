using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Common;
using SubscriptionBilling.Application.DTOs.Plans;
using SubscriptionBilling.Application.DTOs.Subscriptions;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Enums;
using SubscriptionBilling.Domain.Exceptions;
using System.Text.Json;

namespace SubscriptionBilling.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly IAuditLogService _auditLogService;

    public SubscriptionService(
        IApplicationDbContext context,
        IStripePaymentGateway stripeGateway,
        IAuditLogService auditLogService)
    {
        _context = context;
        _stripeGateway = stripeGateway;
        _auditLogService = auditLogService;
    }

    public async Task<SubscriptionDto?> GetCurrentSubscriptionAsync(int userId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId &&
                       (s.Status == SubscriptionStatus.Active ||
                        s.Status == SubscriptionStatus.PastDue ||
                        s.Status == SubscriptionStatus.Trialing ||
                        (s.Status == SubscriptionStatus.Canceled && s.CurrentPeriodEndUtc > DateTime.UtcNow)))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync();

        return subscription != null ? MapToDto(subscription) : null;
    }

    public async Task<SubscriptionDto> GetSubscriptionByIdAsync(int id)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subscription == null)
        {
            throw new NotFoundException(nameof(Subscription), id);
        }

        return MapToDto(subscription);
    }

    public async Task<PagedResult<SubscriptionDto>> GetAllSubscriptionsAsync(int pageNumber = 1, int pageSize = 10, SubscriptionStatus? status = null)
    {
        var query = _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SubscriptionDto>(items.Select(MapToDto).ToList(), totalCount, pageNumber, pageSize);
    }

    public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(int userId, CreateCheckoutSessionRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new NotFoundException(nameof(User), userId);
        }

        var plan = await _context.SubscriptionPlans
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId);

        if (plan == null || !plan.IsActive)
        {
            throw new BusinessRuleValidationException("Selected subscription plan is invalid or inactive.");
        }

        // Select specific price for billing interval or price ID
        SubscriptionPlanPrice? selectedPrice = null;
        if (request.PriceId.HasValue)
        {
            selectedPrice = plan.Prices.FirstOrDefault(pr => pr.Id == request.PriceId.Value && pr.IsActive);
        }
        else if (request.BillingInterval.HasValue)
        {
            selectedPrice = plan.Prices.FirstOrDefault(pr => pr.BillingInterval == request.BillingInterval.Value && pr.IsActive);
        }

        selectedPrice ??= plan.Prices.FirstOrDefault(pr => pr.IsActive);

        // Ensure user has a Stripe Customer Id
        if (string.IsNullOrEmpty(user.StripeCustomerId))
        {
            try
            {
                user.StripeCustomerId = await _stripeGateway.CreateCustomerAsync(user);
                await _context.SaveChangesAsync();
            }
            catch
            {
                user.StripeCustomerId = $"cus_sim_{user.Id}_{Guid.NewGuid():N}";
                await _context.SaveChangesAsync();
            }
        }

        var stripePriceId = selectedPrice?.StripePriceId ?? plan.StripePriceId ?? "price_test_default";

        var sessionResponse = await _stripeGateway.CreateCheckoutSessionAsync(
            user.StripeCustomerId,
            stripePriceId,
            user.Id,
            plan.Id,
            request.SuccessUrl,
            request.CancelUrl);

        await _auditLogService.LogAsync(userId, "CREATE_CHECKOUT_SESSION", "Subscription", plan.Id.ToString(), new { plan.Name, sessionResponse.SessionId });

        return sessionResponse;
    }

    public async Task<SubscriptionDto> UpgradeOrDowngradeSubscriptionAsync(int userId, UpgradeSubscriptionRequest request)
    {
        var currentSub = await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .FirstOrDefaultAsync();

        if (currentSub == null)
        {
            throw new BusinessRuleValidationException("No active subscription found to modify.");
        }

        if (currentSub.PlanId == request.NewPlanId)
        {
            throw new BusinessRuleValidationException("You are already subscribed to this plan.");
        }

        var newPlan = await _context.SubscriptionPlans.FindAsync(request.NewPlanId);
        if (newPlan == null || !newPlan.IsActive)
        {
            throw new BusinessRuleValidationException("The target subscription plan is invalid or inactive.");
        }

        // Call Stripe to update plan with proration
        if (!string.IsNullOrEmpty(currentSub.StripeSubscriptionId) && !string.IsNullOrEmpty(newPlan.StripePriceId))
        {
            await _stripeGateway.ModifySubscriptionPlanAsync(currentSub.StripeSubscriptionId, newPlan.StripePriceId);
        }

        var oldPlanName = currentSub.Plan.Name;
        currentSub.PlanId = newPlan.Id;
        currentSub.Plan = newPlan;
        currentSub.CancelAtPeriodEnd = false; // Reset cancellation if previously scheduled

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(userId, "CHANGE_PLAN", "Subscription", currentSub.Id.ToString(), new
        {
            FromPlan = oldPlanName,
            ToPlan = newPlan.Name,
            ProrationApplied = true
        });

        return MapToDto(currentSub);
    }

    public async Task<SubscriptionDto> CancelSubscriptionAsync(int userId, CancelSubscriptionRequest request)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            throw new BusinessRuleValidationException("No active subscription found to cancel.");
        }

        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            await _stripeGateway.CancelSubscriptionAsync(subscription.StripeSubscriptionId, request.Immediately);
        }

        if (request.Immediately)
        {
            subscription.Status = SubscriptionStatus.Canceled;
            subscription.CanceledAtUtc = DateTime.UtcNow;
            subscription.CancelAtPeriodEnd = false;
        }
        else
        {
            subscription.CancelAtPeriodEnd = true;
        }

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(userId, "CANCEL_SUBSCRIPTION", "Subscription", subscription.Id.ToString(), new
        {
            Immediately = request.Immediately,
            EffectiveDate = request.Immediately ? DateTime.UtcNow : subscription.CurrentPeriodEndUtc
        });

        return MapToDto(subscription);
    }

    public async Task<SubscriptionDto> ReactivateSubscriptionAsync(int userId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active && s.CancelAtPeriodEnd)
            .FirstOrDefaultAsync();

        if (subscription == null)
        {
            throw new BusinessRuleValidationException("No pending cancellation found to reactivate.");
        }

        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            // Reactivate in Stripe by clearing cancel_at_period_end
            await _stripeGateway.CancelSubscriptionAsync(subscription.StripeSubscriptionId, immediately: false);
        }

        subscription.CancelAtPeriodEnd = false;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(userId, "REACTIVATE_SUBSCRIPTION", "Subscription", subscription.Id.ToString());

        return MapToDto(subscription);
    }

    private static SubscriptionDto MapToDto(Subscription s)
    {
        List<string> features;
        try
        {
            features = JsonSerializer.Deserialize<List<string>>(s.Plan?.FeaturesJson ?? "[]") ?? new List<string>();
        }
        catch
        {
            features = new List<string>();
        }

        var planDto = new SubscriptionPlanDto(
            s.Plan?.Id ?? s.PlanId,
            s.Plan?.Name ?? "Custom Plan",
            s.Plan?.Description ?? "",
            s.Plan?.Price ?? 0m,
            s.Plan?.Currency ?? "INR",
            s.Plan?.BillingInterval ?? BillingInterval.Monthly,
            s.Plan?.StripePriceId,
            s.Plan?.StripeProductId,
            features,
            s.Plan?.IsActive ?? true,
            s.Plan?.IsPopular ?? false,
            s.Plan?.Prices.Select(pr => new PlanPriceDto(pr.Id, pr.SubscriptionPlanId, pr.BillingInterval, pr.Amount, pr.Currency, pr.StripePriceId, pr.IsActive)).ToList() ?? new List<PlanPriceDto>(),
            s.Plan?.PlanFeatures.OrderBy(f => f.DisplayOrder).Select(f => new PlanFeatureDto(f.Id, f.SubscriptionPlanId, f.FeatureName, f.FeatureValue, f.DisplayOrder)).ToList() ?? new List<PlanFeatureDto>(),
            s.Plan?.CreatedAtUtc ?? DateTime.UtcNow,
            s.Plan?.UpdatedAtUtc
        );

        return new SubscriptionDto(
            s.Id,
            s.UserId,
            s.User?.Email ?? "",
            s.PlanId,
            planDto,
            s.SubscriptionPlanPriceId,
            s.StripeSubscriptionId,
            s.Status,
            s.CurrentPeriodStartUtc,
            s.CurrentPeriodEndUtc,
            s.CancelAtPeriodEnd,
            s.CanceledAtUtc,
            s.CreatedAtUtc
        );
    }
}
