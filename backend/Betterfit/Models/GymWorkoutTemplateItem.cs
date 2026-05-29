using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymWorkoutTemplateItem
{
    public Guid Id { get; set; }

    public Guid GymWorkoutTemplateId { get; set; }

    public Guid? ExerciseId { get; set; }

    public int DayNumber { get; set; }

    public int SortOrder { get; set; }

    [MaxLength(160)]
    public string ExerciseName { get; set; } = string.Empty;

    [MaxLength(32)]
    public string SetsPrescription { get; set; } = string.Empty;

    [MaxLength(32)]
    public string RepetitionsPrescription { get; set; } = string.Empty;

    public int? RestSeconds { get; set; }

    [MaxLength(32)]
    public string? Tempo { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public GymWorkoutTemplate Template { get; set; } = null!;

    public GymWorkoutExercise? Exercise { get; set; }
}
