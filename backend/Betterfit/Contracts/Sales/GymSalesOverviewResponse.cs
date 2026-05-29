namespace Betterfit.Contracts.Sales;

public sealed record GymSalesOverviewResponse(
    int SalesTodayCount,
    decimal RevenueTodayAmount,
    decimal RevenueMonthAmount,
    decimal PendingCollectionAmount,
    int RenewalCandidatesCount,
    int FailedPaymentsCount);
