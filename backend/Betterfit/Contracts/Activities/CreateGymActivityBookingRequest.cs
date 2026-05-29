using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Activities;

public sealed class CreateGymActivityBookingRequest
{
    [Required]
    public Guid MembershipId { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
