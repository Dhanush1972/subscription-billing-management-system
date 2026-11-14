using SubscriptionBilling.Application.DTOs.Dashboard;

namespace SubscriptionBilling.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardMetricsDto> GetDashboardMetricsAsync();
    Task<IReadOnlyList<RevenueChartPointDto>> GetRevenueChartAsync(int months = 6);
    Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(int limit = 10);
}
