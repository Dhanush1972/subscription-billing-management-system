using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Application.DTOs.Dashboard;
using SubscriptionBilling.Application.Interfaces;

namespace SubscriptionBilling.API.Controllers;

[Authorize(Roles = "Admin")]
public class DashboardController : BaseApiController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("metrics")]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics()
    {
        var metrics = await _dashboardService.GetDashboardMetricsAsync();
        return Ok(metrics);
    }

    [HttpGet("revenue-chart")]
    [ProducesResponseType(typeof(IReadOnlyList<RevenueChartPointDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRevenueChart([FromQuery] int months = 6)
    {
        var chart = await _dashboardService.GetRevenueChartAsync(months);
        return Ok(chart);
    }

    [HttpGet("recent-activity")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentActivityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int limit = 10)
    {
        var activity = await _dashboardService.GetRecentActivityAsync(limit);
        return Ok(activity);
    }
}
