using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Training;

public sealed class CreateGymWorkoutExerciseRequest
{
    [Required]
    [MaxLength(160)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(80)]
    public string? Category { get; init; }

    [MaxLength(80)]
    public string? MuscleGroup { get; init; }

    [MaxLength(120)]
    public string? Equipment { get; init; }

    [MaxLength(2000)]
    public string? Instructions { get; init; }

    [MaxLength(256)]
    public string? VideoUrl { get; init; }
}
