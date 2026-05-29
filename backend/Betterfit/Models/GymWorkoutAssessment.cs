using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymWorkoutAssessment
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid GymMembershipId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? CoachAssignmentId { get; set; }

    public string RecordedByUserId { get; set; } = string.Empty;

    public DateTime RecordedAtUtc { get; set; }

    public decimal? WeightKg { get; set; }

    public decimal? BodyFatPercentage { get; set; }

    public decimal? LeanMassKg { get; set; }

    public decimal? ChestCm { get; set; }

    public decimal? WaistCm { get; set; }

    public decimal? HipsCm { get; set; }

    public decimal? ArmCm { get; set; }

    public decimal? ThighCm { get; set; }

    public decimal? CalfCm { get; set; }

    public int? RestingHeartRateBpm { get; set; }

    [MaxLength(1500)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymMembership Membership { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public TenantRoleAssignment? CoachAssignment { get; set; }

    public ApplicationUser RecordedByUser { get; set; } = null!;
}
