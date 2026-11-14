using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.DTOs.Dashboard;
using SubscriptionBilling.Application.Interfaces;
using SubscriptionBilling.Domain.Enums;

namespace SubscriptionBilling.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _context;

    public DashboardService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync()
    {
        var totalCustomers = await _context.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role.Name == "Customer"))
            .CountAsync();

        var activeSubs = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active)
            .ToListAsync();

        var activeCount = activeSubs.Count;

        var canceledCount = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Canceled)
            .CountAsync();

        // Calculate MRR from actual active subscriptions: Monthly plan price + (Yearly plan price / 12)
        decimal mrr = 0m;
        foreach (var sub in activeSubs)
        {
            if (sub.Plan != null)
            {
                if (sub.Plan.BillingInterval == BillingInterval.Yearly)
                {
                    mrr += Math.Round(sub.Plan.Price / 12m, 2);
                }
                else
                {
                    mrr += sub.Plan.Price;
                }
            }
        }

        var totalRevenue = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        var successCount = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .CountAsync();

        var failedCount = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Failed)
            .CountAsync();

        return new DashboardMetricsDto(
            totalCustomers,
            activeCount,
            canceledCount,
            mrr,
            totalRevenue,
            successCount,
            failedCount
        );
    }

    public async Task<IReadOnlyList<RevenueChartPointDto>> GetRevenueChartAsync(int months = 6)
    {
        var startDate = DateTime.UtcNow.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var payments = await _context.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded && p.PaymentDateUtc >= startDate)
            .ToListAsync();

        var subscriptions = await _context.Subscriptions
            .Where(s => s.CreatedAtUtc >= startDate)
            .ToListAsync();

        var points = new List<RevenueChartPointDto>();

        for (int i = 0; i < months; i++)
        {
            var currentMonth = startDate.AddMonths(i);
            var periodKey = currentMonth.ToString("yyyy-MM");

            var monthRevenue = payments
                .Where(p => p.PaymentDateUtc.Year == currentMonth.Year && p.PaymentDateUtc.Month == currentMonth.Month)
                .Sum(p => p.Amount);

            var monthSubs = subscriptions
                .Count(s => s.CreatedAtUtc.Year == currentMonth.Year && s.CreatedAtUtc.Month == currentMonth.Month);

            points.Add(new RevenueChartPointDto(periodKey, monthRevenue, monthSubs));
        }

        return points;
    }

    public async Task<IReadOnlyList<RecentActivityDto>> GetRecentActivityAsync(int limit = 10)
    {
        var recentPayments = await _context.Payments
            .Include(p => p.User)
            .OrderByDescending(p => p.PaymentDateUtc)
            .Take(limit)
            .Select(p => new RecentActivityDto(
                "Payment",
                $"{p.User.Email} paid ${p.Amount:F2} via {p.PaymentMethodSummary ?? "Card"}",
                p.Amount,
                p.Status.ToString(),
                p.PaymentDateUtc
            ))
            .ToListAsync();

        var recentSubs = await _context.Subscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(limit)
            .Select(s => new RecentActivityDto(
                "Subscription",
                $"{s.User.Email} subscribed to {s.Plan.Name}",
                s.Plan.Price,
                s.Status.ToString(),
                s.CreatedAtUtc
            ))
            .ToListAsync();

        var activities = recentPayments
            .Concat(recentSubs)
            .OrderByDescending(a => a.TimestampUtc)
            .Take(limit)
            .ToList();

        return activities;
    }
}
