namespace SubscriptionBilling.Application.DTOs.Dashboard;

public record DashboardMetricsDto(
    int TotalCustomers,
    int ActiveSubscriptions,
    int CanceledSubscriptions,
    decimal MonthlyRecurringRevenue,
    decimal TotalRevenue,
    int SuccessfulPaymentsCount,
    int FailedPaymentsCount
);

public record RevenueChartPointDto(
    string Period, // e.g. "2026-10", "2026-11"
    decimal Revenue,
    int SubscriptionsCount
);

public record RecentActivityDto(
    string Type, // "Subscription", "Payment", "Invoice"
    string Description,
    decimal? Amount,
    string Status,
    DateTime TimestampUtc
);
