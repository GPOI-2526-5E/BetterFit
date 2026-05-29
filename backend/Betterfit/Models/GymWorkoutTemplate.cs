using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymWorkoutTemplate
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? CoachAssignmentId { get; set; }

    [MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Goal { get; set; }

    public GymWorkoutTemplateLevel Level { get; set; } = GymWorkoutTemplateLevel.Mixed;

    [MaxLength(1500)]
    public string? Description { get; set; }

    public int DaysPerWeek { get; set; } = 3;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public TenantRoleAssignment? CoachAssignment { get; set; }

    public ICollection<GymWorkoutTemplateItem> Items { get; set; } = new List<GymWorkoutTemplateItem>();

    public ICollection<GymWorkoutAssignment> Assignments { get; set; } = new List<GymWorkoutAssignment>();
}
