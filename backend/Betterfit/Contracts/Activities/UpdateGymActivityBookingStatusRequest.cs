using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Activities;

public sealed class UpdateGymActivityBookingStatusRequest
{
    [Required]
    public GymActivityBookingStatus Status { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
