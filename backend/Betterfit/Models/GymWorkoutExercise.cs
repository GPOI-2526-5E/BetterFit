using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymWorkoutExercise
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Category { get; set; }

    [MaxLength(80)]
    public string? MuscleGroup { get; set; }

    [MaxLength(120)]
    public string? Equipment { get; set; }

    [MaxLength(2000)]
    public string? Instructions { get; set; }

    [MaxLength(256)]
    public string? VideoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public ICollection<GymWorkoutTemplateItem> TemplateItems { get; set; } = new List<GymWorkoutTemplateItem>();

    public ICollection<GymWorkoutAssignmentItem> AssignmentItems { get; set; } = new List<GymWorkoutAssignmentItem>();
}
