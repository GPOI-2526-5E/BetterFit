using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed record GymAutomationRuleResponse(
    Guid AutomationRuleId,
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
    GymAutomationScheduleType ScheduleType,
    GymAutomationStatus Status,
    string SubjectTemplate,
    string MessageTemplate,
    string? Notes,
    DateTime NextRunAtUtc,
    DateTime? LastRunAtUtc,
    int EstimatedAudienceCount,
    int? LastAudienceCount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
