using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed record GymLeadStageSummaryResponse(
    GymLeadStage Stage,
    int LeadsCount);
