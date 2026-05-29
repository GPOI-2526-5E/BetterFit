using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymAutomationRule
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? OwnerAssignmentId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    public GymCampaignChannel Channel { get; set; } = GymCampaignChannel.Email;

    public GymCampaignAudienceType AudienceType { get; set; } = GymCampaignAudienceType.ActiveMembers;

    public GymLeadStage? TargetLeadStage { get; set; }

    public GymAutomationScheduleType ScheduleType { get; set; } = GymAutomationScheduleType.Daily;

    public GymAutomationStatus Status { get; set; } = GymAutomationStatus.Active;

    public DateTime NextRunAtUtc { get; set; }

    [MaxLength(180)]
    public string SubjectTemplate { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string MessageTemplate { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime? LastRunAtUtc { get; set; }

    public int? LastAudienceCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public TenantRoleAssignment? OwnerAssignment { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;
}
