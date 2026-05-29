using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Crm;

public sealed class CreateGymLeadTaskRequest
{
    public Guid? AssignedAssignmentId { get; init; }

    [Required]
    [MaxLength(160)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; init; }

    public DateTime? DueAtUtc { get; init; }
}
