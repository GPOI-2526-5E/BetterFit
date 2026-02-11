using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Gyms;

/// <summary>
/// Request payload used to create a gym.
/// </summary>
public class CreateGymRequest
{
    /// <summary>
    /// Gym name.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;
}
