namespace Betterfit.Contracts.Access;

public sealed record GymAccessOverviewResponse(
    int PeoplePresentTodayCount,
    int CheckinsLast30MinutesCount,
    int DeniedTodayCount,
    int DeskApprovalsTodayCount,
    IReadOnlyCollection<GymAccessEventResponse> RecentEvents,
    IReadOnlyCollection<GymAccessDeniedAttemptResponse> DeniedAttempts,
    DateTime LastSyncUtc);
