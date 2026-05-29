using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Activities;

public sealed class CreateGymActivitySessionRequest
{
    [Required]
    public Guid TemplateId { get; init; }

    [Required]
    public DateTime StartsAtUtc { get; init; }

    public DateTime? EndsAtUtc { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}
