using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Training;

public sealed class CreateGymWorkoutAssessmentRequest
{
    [Required]
    public Guid MembershipId { get; init; }

    [Required]
    public Guid LocationId { get; init; }

    public Guid? CoachAssignmentId { get; init; }

    public DateTime? RecordedAtUtc { get; init; }

    [Range(0, 1000)]
    public decimal? WeightKg { get; init; }

    [Range(0, 100)]
    public decimal? BodyFatPercentage { get; init; }

    [Range(0, 1000)]
    public decimal? LeanMassKg { get; init; }

    [Range(0, 500)]
    public decimal? ChestCm { get; init; }

    [Range(0, 500)]
    public decimal? WaistCm { get; init; }

    [Range(0, 500)]
    public decimal? HipsCm { get; init; }

    [Range(0, 250)]
    public decimal? ArmCm { get; init; }

    [Range(0, 250)]
    public decimal? ThighCm { get; init; }

    [Range(0, 250)]
    public decimal? CalfCm { get; init; }

    [Range(0, 300)]
    public int? RestingHeartRateBpm { get; init; }

    [MaxLength(1500)]
    public string? Notes { get; init; }
}
