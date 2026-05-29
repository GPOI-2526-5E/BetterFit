namespace Betterfit.Contracts.Dashboard;

public sealed record GymDashboardOverviewResponse(
    int AccessesTodayCount,
    decimal RevenueTodayAmount,
    int ExpiringMembershipsCount,
    int PendingActivationsCount,
    decimal PendingCollectionsAmount,
    int DeniedTodayCount,
    int FailedPaymentsCount,
    bool CanReadMembers,
    bool CanReadBilling,
    bool CanReadCheckins,
    IReadOnlyCollection<GymDashboardTaskResponse> Tasks,
    IReadOnlyCollection<GymDashboardAlertResponse> Alerts,
    IReadOnlyCollection<GymDashboardStatusCardResponse> StatusCards,
    IReadOnlyCollection<GymDashboardDeviceResponse> Devices,
    IReadOnlyCollection<GymDashboardTimelineItemResponse> Timeline,
    IReadOnlyCollection<GymDashboardLocationSnapshotResponse> Locations,
    IReadOnlyCollection<GymDashboardRecentCollectionResponse> RecentCollections,
    DateTime GeneratedAtUtc);

public sealed record GymDashboardTaskResponse(
    string Title,
    string MemberName,
    string Status,
    DateTime? DueAtUtc,
    string Detail);

public sealed record GymDashboardAlertResponse(
    string Title,
    string Description,
    string Status);

public sealed record GymDashboardTimelineItemResponse(
    DateTime OccurredAtUtc,
    string Text);

public sealed record GymDashboardStatusCardResponse(
    string Id,
    string Label,
    string Value,
    string? Description,
    string Tone);

public sealed record GymDashboardDeviceResponse(
    string Name,
    string Status,
    string LastEvent);

public sealed record GymDashboardLocationSnapshotResponse(
    Guid LocationId,
    string LocationName,
    int AccessesTodayCount,
    decimal RevenueTodayAmount,
    int ExpiringMembershipsCount,
    int PendingActivationsCount);

public sealed record GymDashboardRecentCollectionResponse(
    Guid PaymentId,
    DateTime PaidAtUtc,
    string ReferenceCode,
    string ReceiptCode,
    string MemberName,
    string LocationName,
    decimal Amount,
    string Method);
