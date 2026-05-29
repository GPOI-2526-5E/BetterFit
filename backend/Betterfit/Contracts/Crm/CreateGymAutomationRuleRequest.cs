using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed record CreateGymAutomationRuleRequest(
    Guid LocationId,
    Guid? OwnerAssignmentId,
    string Name,
    GymCampaignChannel Channel,
    GymCampaignAudienceType AudienceType,
    GymLeadStage? TargetLeadStage,
    GymAutomationScheduleType ScheduleType,
    DateTime NextRunAtUtc,
    string SubjectTemplate,
    string MessageTemplate,
    string? Notes);
