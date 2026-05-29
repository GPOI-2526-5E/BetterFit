using Betterfit.Models;

namespace Betterfit.Contracts.Reports;

public sealed record GymKpiDashboardResponse(
    int ActiveMembersCount,
    decimal RevenueMonthAmount,
    decimal PendingCollectionsAmount,
    int LeadsInPipelineCount,
    decimal LeadConversionRatePercentage,
    int AccessesTodayCount,
    int UpcomingBookingsCount,
    int ActiveWorkoutAssignmentsCount,
    IReadOnlyCollection<GymKpiLocationRevenueResponse> RevenueByLocation,
    IReadOnlyCollection<GymKpiLocationAccessResponse> AccessByLocation,
    IReadOnlyCollection<GymKpiUpcomingActivityResponse> UpcomingActivities,
    IReadOnlyCollection<GymKpiLeadStageResponse> LeadPipeline,
    IReadOnlyCollection<GymKpiTrainingSummaryResponse> TrainingByLocation,
    DateTime GeneratedAtUtc);

public sealed record GymKpiLocationRevenueResponse(
    Guid LocationId,
    string LocationName,
    int SalesCount,
    decimal RevenueMonthAmount,
    decimal PendingCollectionsAmount);

public sealed record GymKpiLocationAccessResponse(
    Guid LocationId,
    string LocationName,
    int GrantedTodayCount,
    int DeniedTodayCount);

public sealed record GymKpiUpcomingActivityResponse(
    Guid SessionId,
    Guid LocationId,
    string LocationName,
    string Title,
    string InstructorName,
    DateTime StartsAtUtc,
    int Capacity,
    int BookedCount,
    decimal OccupancyRatePercentage,
    GymActivitySessionStatus Status);

public sealed record GymKpiLeadStageResponse(
    GymLeadStage Stage,
    int LeadsCount);

public sealed record GymKpiTrainingSummaryResponse(
    Guid LocationId,
    string LocationName,
    int ActiveAssignmentsCount,
    int RevisionDueCount,
    int AssessmentsLast30DaysCount);
