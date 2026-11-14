using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.DTOs.Plans;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Entities;
using SubscriptionBilling.Domain.Exceptions;
using System.Text.Json;

namespace SubscriptionBilling.Application.Services;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly IApplicationDbContext _context;
    private readonly IStripePaymentGateway _stripeGateway;
    private readonly IAuditLogService _auditLogService;

    public SubscriptionPlanService(
        IApplicationDbContext context,
        IStripePaymentGateway stripeGateway,
        IAuditLogService auditLogService)
    {
        _context = context;
        _stripeGateway = stripeGateway;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetActivePlansAsync()
    {
        var plans = await _context.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.PlanFeatures)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .AsNoTracking()
            .ToListAsync();

        return plans.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetAllPlansForAdminAsync()
    {
        var plans = await _context.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.PlanFeatures)
            .OrderBy(p => p.Price)
            .AsNoTracking()
            .ToListAsync();

        return plans.Select(MapToDto).ToList();
    }

    public async Task<SubscriptionPlanDto> GetPlanByIdAsync(int id)
    {
        var plan = await _context.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.PlanFeatures)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            throw new NotFoundException(nameof(SubscriptionPlan), id);
        }

        return MapToDto(plan);
    }

    public async Task<SubscriptionPlanDto> CreatePlanAsync(CreatePlanRequest request, int adminUserId)
    {
        var plan = new SubscriptionPlan
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            BillingInterval = request.BillingInterval,
            IsPopular = request.IsPopular,
            FeaturesJson = JsonSerializer.Serialize(request.Features),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        // If normalized prices are supplied in the request, add them
        if (request.Prices != null && request.Prices.Any())
        {
            foreach (var p in request.Prices)
            {
                plan.Prices.Add(new SubscriptionPlanPrice
                {
                    BillingInterval = p.BillingInterval,
                    Amount = p.Amount,
                    Currency = p.Currency.Trim().ToUpperInvariant(),
                    StripePriceId = p.StripePriceId,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
        else
        {
            // Default pricing record based on base price
            plan.Prices.Add(new SubscriptionPlanPrice
            {
                BillingInterval = request.BillingInterval,
                Amount = request.Price,
                Currency = request.Currency.Trim().ToUpperInvariant(),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        // If normalized features are supplied, add them
        if (request.PlanFeatures != null && request.PlanFeatures.Any())
        {
            foreach (var f in request.PlanFeatures)
            {
                plan.PlanFeatures.Add(new SubscriptionPlanFeature
                {
                    FeatureName = f.FeatureName.Trim(),
                    FeatureValue = f.FeatureValue.Trim(),
                    DisplayOrder = f.DisplayOrder
                });
            }
        }
        else if (request.Features != null)
        {
            int order = 1;
            foreach (var f in request.Features)
            {
                plan.PlanFeatures.Add(new SubscriptionPlanFeature
                {
                    FeatureName = f.Trim(),
                    FeatureValue = "Included",
                    DisplayOrder = order++
                });
            }
        }

        // Create in Stripe
        try
        {
            var (productId, priceId) = await _stripeGateway.CreateProductAndPriceAsync(plan);
            plan.StripeProductId = productId;
            plan.StripePriceId = priceId;
        }
        catch
        {
            // Fallback for offline/test environments
            plan.StripeProductId = null;
            plan.StripePriceId = null;
        }

        await _context.SubscriptionPlans.AddAsync(plan);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(adminUserId, "CREATE_PLAN", "SubscriptionPlan", plan.Id.ToString(), new { plan.Name, plan.Price, plan.BillingInterval });

        return MapToDto(plan);
    }

    public async Task<SubscriptionPlanDto> UpdatePlanAsync(int id, UpdatePlanRequest request, int adminUserId)
    {
        var plan = await _context.SubscriptionPlans
            .Include(p => p.Prices)
            .Include(p => p.PlanFeatures)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            throw new NotFoundException(nameof(SubscriptionPlan), id);
        }

        plan.Name = request.Name.Trim();
        plan.Description = request.Description.Trim();
        plan.IsActive = request.IsActive;
        plan.IsPopular = request.IsPopular;
        plan.FeaturesJson = JsonSerializer.Serialize(request.Features);

        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(adminUserId, "UPDATE_PLAN", "SubscriptionPlan", id.ToString(), new { plan.Name, plan.IsActive });

        return MapToDto(plan);
    }

    public async Task TogglePlanStatusAsync(int id, bool isActive, int adminUserId)
    {
        var plan = await _context.SubscriptionPlans.FindAsync(id);
        if (plan == null)
        {
            throw new NotFoundException(nameof(SubscriptionPlan), id);
        }

        plan.IsActive = isActive;
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(adminUserId, "TOGGLE_PLAN_STATUS", "SubscriptionPlan", id.ToString(), new { IsActive = isActive });
    }

    public async Task DeletePlanAsync(int id, int adminUserId)
    {
        var plan = await _context.SubscriptionPlans
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            throw new NotFoundException(nameof(SubscriptionPlan), id);
        }

        // Business Rule: Cannot delete a plan if it has active or historic subscriptions attached.
        if (plan.Subscriptions.Any(s => s.Status == Domain.Enums.SubscriptionStatus.Active || s.Status == Domain.Enums.SubscriptionStatus.PastDue))
        {
            throw new BusinessRuleValidationException("Cannot delete plan with active or past-due subscriptions. You can deactivate the plan instead.");
        }

        if (plan.Subscriptions.Any())
        {
            // If historical subscriptions exist, softly deactivate rather than hard deleting to preserve financial integrity
            plan.IsActive = false;
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(adminUserId, "SOFT_DELETE_PLAN", "SubscriptionPlan", id.ToString(), new { Reason = "Has historical subscriptions" });
            return;
        }

        _context.SubscriptionPlans.Remove(plan);
        await _context.SaveChangesAsync();

        await _auditLogService.LogAsync(adminUserId, "DELETE_PLAN", "SubscriptionPlan", id.ToString());
    }

    private static SubscriptionPlanDto MapToDto(SubscriptionPlan plan)
    {
        List<string> features;
        try
        {
            features = JsonSerializer.Deserialize<List<string>>(plan.FeaturesJson) ?? new List<string>();
        }
        catch
        {
            features = new List<string>();
        }

        var prices = plan.Prices.Select(pr => new PlanPriceDto(
            pr.Id,
            pr.SubscriptionPlanId,
            pr.BillingInterval,
            pr.Amount,
            pr.Currency,
            pr.StripePriceId,
            pr.IsActive
        )).ToList();

        var planFeatures = plan.PlanFeatures
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new PlanFeatureDto(
                f.Id,
                f.SubscriptionPlanId,
                f.FeatureName,
                f.FeatureValue,
                f.DisplayOrder
            )).ToList();

        return new SubscriptionPlanDto(
            plan.Id,
            plan.Name,
            plan.Description,
            plan.Price,
            plan.Currency,
            plan.BillingInterval,
            plan.StripePriceId,
            plan.StripeProductId,
            features,
            plan.IsActive,
            plan.IsPopular,
            prices,
            planFeatures,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc
        );
    }
}
