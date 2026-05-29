using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymCampaign
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

    public GymCampaignStatus Status { get; set; } = GymCampaignStatus.Draft;

    [MaxLength(180)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime? ScheduledAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public int? LastAudienceCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public TenantRoleAssignment? OwnerAssignment { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;
}
