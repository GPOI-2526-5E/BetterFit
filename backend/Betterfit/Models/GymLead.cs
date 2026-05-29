using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymLead
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? OwnerAssignmentId { get; set; }

    public Guid? ConvertedMembershipId { get; set; }

    [MaxLength(160)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(32)]
    public string? PhoneNumber { get; set; }

    public GymLeadSource Source { get; set; } = GymLeadSource.Other;

    public GymLeadStage Stage { get; set; } = GymLeadStage.New;

    [MaxLength(200)]
    public string? Interest { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime? LastContactedAtUtc { get; set; }

    public DateTime? NextFollowUpAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public TenantRoleAssignment? OwnerAssignment { get; set; }

    public GymMembership? ConvertedMembership { get; set; }

    public ICollection<GymLeadTask> Tasks { get; set; } = new List<GymLeadTask>();
}
