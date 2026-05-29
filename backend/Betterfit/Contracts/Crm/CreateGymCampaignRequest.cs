using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed record CreateGymCampaignRequest(
    Guid LocationId,
    Guid? OwnerAssignmentId,
    string Name,
    GymCampaignChannel Channel,
    GymCampaignAudienceType AudienceType,
    GymLeadStage? TargetLeadStage,
    string Subject,
    string Message,
    string? Notes,
    DateTime? ScheduledAtUtc);
