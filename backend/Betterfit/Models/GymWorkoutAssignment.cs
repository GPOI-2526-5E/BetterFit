using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymWorkoutAssignment
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid GymMembershipId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? GymWorkoutTemplateId { get; set; }

    public Guid? CoachAssignmentId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;

    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Goal { get; set; }

    public GymWorkoutAssignmentStatus Status { get; set; } = GymWorkoutAssignmentStatus.Active;

    public DateTime AssignedAtUtc { get; set; }

    public DateTime? StartsAtUtc { get; set; }

    public DateTime? RevisionDueAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    [MaxLength(1500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymMembership Membership { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public GymWorkoutTemplate? Template { get; set; }

    public TenantRoleAssignment? CoachAssignment { get; set; }

    public ApplicationUser CreatedByUser { get; set; } = null!;

    public ICollection<GymWorkoutAssignmentItem> Items { get; set; } = new List<GymWorkoutAssignmentItem>();
}
