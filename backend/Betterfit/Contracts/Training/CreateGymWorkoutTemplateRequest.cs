using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Training;

public sealed class CreateGymWorkoutTemplateRequest
{
    [Required]
    public Guid LocationId { get; init; }

    public Guid? CoachAssignmentId { get; init; }

    [Required]
    [MaxLength(160)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(120)]
    public string? Goal { get; init; }

    public GymWorkoutTemplateLevel Level { get; init; } = GymWorkoutTemplateLevel.Mixed;

    [MaxLength(1500)]
    public string? Description { get; init; }

    [Range(1, 14)]
    public int DaysPerWeek { get; init; } = 3;

    [MinLength(1)]
    public IReadOnlyCollection<CreateGymWorkoutTemplateItemRequest> Items { get; init; } = [];
}
