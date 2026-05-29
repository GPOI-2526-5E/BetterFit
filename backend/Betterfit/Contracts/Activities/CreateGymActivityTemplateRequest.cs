using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Activities;

public sealed class CreateGymActivityTemplateRequest
{
    [Required]
    public Guid LocationId { get; init; }

    public Guid? InstructorAssignmentId { get; init; }

    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(80)]
    public string Category { get; init; } = "Corso";

    [MaxLength(1000)]
    public string? Description { get; init; }

    [MaxLength(24)]
    public string? ColorHex { get; init; }

    [Range(1, 500)]
    public int Capacity { get; init; } = 20;

    [Range(15, 480)]
    public int DurationMinutes { get; init; } = 60;

    public bool RequiresBooking { get; init; } = true;
}
