using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Training;

public sealed class UpdateGymWorkoutTemplateActivationRequest
{
    [Required]
    public bool IsActive { get; init; }
}
