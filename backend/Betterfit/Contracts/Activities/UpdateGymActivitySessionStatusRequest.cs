using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Activities;

public sealed class UpdateGymActivitySessionStatusRequest
{
    [Required]
    public GymActivitySessionStatus Status { get; init; }
}
