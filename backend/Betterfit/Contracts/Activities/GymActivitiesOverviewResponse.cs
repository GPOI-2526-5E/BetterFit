namespace Betterfit.Contracts.Activities;

public sealed record GymActivitiesOverviewResponse(
    int SessionsNext7DaysCount,
    int BookingsNext7DaysCount,
    int CheckedInTodayCount,
    int NoShowLast30DaysCount,
    IReadOnlyCollection<GymActivitySessionResponse> UpcomingSessions,
    DateTime GeneratedAtUtc);
