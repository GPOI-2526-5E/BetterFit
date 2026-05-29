using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed record GymCampaignResponse(
    Guid CampaignId,
    Guid GymId,
    Guid LocationId,
    string LocationName,
    Guid? OwnerAssignmentId,
    string? OwnerName,
    string CreatedByUserId,
    string CreatedByUserName,
    string Name,
    GymCampaignChannel Channel,
    GymCampaignAudienceType AudienceType,
    GymLeadStage? TargetLeadStage,
    GymCampaignStatus Status,
    string Subject,
    string Message,
    string? Notes,
    DateTime? ScheduledAtUtc,
    DateTime? SentAtUtc,
    int EstimatedAudienceCount,
    int? LastAudienceCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
