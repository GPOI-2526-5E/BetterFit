using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Activities;

public sealed class UpdateGymActivityTemplateActivationRequest
{
    [Required]
    public bool IsActive { get; init; }
}
