namespace Betterfit.Contracts.Crm;

public sealed record GymCrmOverviewResponse(
    int TotalLeads,
    int LeadsNeedingFollowUpCount,
    int LeadsWonThisMonthCount,
    int OpenTasksCount,
    IReadOnlyCollection<GymLeadStageSummaryResponse> Pipeline,
    IReadOnlyCollection<GymLeadResponse> RecentLeads,
    DateTime GeneratedAtUtc);
