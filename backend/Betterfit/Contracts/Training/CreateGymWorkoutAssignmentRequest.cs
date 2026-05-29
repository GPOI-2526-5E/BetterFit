using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Training;

public sealed class CreateGymWorkoutAssignmentRequest
{
    [Required]
    public Guid MembershipId { get; init; }

    [Required]
    public Guid TemplateId { get; init; }

    public DateTime? StartsAtUtc { get; init; }

    public DateTime? RevisionDueAtUtc { get; init; }

    [MaxLength(1500)]
    public string? Notes { get; init; }
}
