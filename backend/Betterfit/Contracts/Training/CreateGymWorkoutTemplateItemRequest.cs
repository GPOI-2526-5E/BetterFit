using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Training;

public sealed class CreateGymWorkoutTemplateItemRequest
{
    public Guid? ExerciseId { get; init; }

    [Required]
    [MaxLength(160)]
    public string ExerciseName { get; init; } = string.Empty;

    [Range(1, 14)]
    public int DayNumber { get; init; }

    [Range(1, 200)]
    public int SortOrder { get; init; }

    [Required]
    [MaxLength(32)]
    public string SetsPrescription { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string RepetitionsPrescription { get; init; } = string.Empty;

    [Range(0, 600)]
    public int? RestSeconds { get; init; }

    [MaxLength(32)]
    public string? Tempo { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
