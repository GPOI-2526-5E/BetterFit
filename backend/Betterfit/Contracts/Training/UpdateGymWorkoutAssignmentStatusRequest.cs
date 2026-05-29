using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Training;

public sealed class UpdateGymWorkoutAssignmentStatusRequest
{
    [Required]
    public GymWorkoutAssignmentStatus Status { get; init; }

    public DateTime? CompletedAtUtc { get; init; }
}
