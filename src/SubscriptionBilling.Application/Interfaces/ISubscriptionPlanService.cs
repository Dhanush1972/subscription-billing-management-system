using SubscriptionBilling.Application.DTOs.Plans;

namespace SubscriptionBilling.Application.Interfaces;

public interface ISubscriptionPlanService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetActivePlansAsync();
    Task<IReadOnlyList<SubscriptionPlanDto>> GetAllPlansForAdminAsync();
    Task<SubscriptionPlanDto> GetPlanByIdAsync(int id);
    Task<SubscriptionPlanDto> CreatePlanAsync(CreatePlanRequest request, int adminUserId);
    Task<SubscriptionPlanDto> UpdatePlanAsync(int id, UpdatePlanRequest request, int adminUserId);
    Task TogglePlanStatusAsync(int id, bool isActive, int adminUserId);
    Task DeletePlanAsync(int id, int adminUserId);
}
