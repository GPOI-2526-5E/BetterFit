using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Gyms;

/// <summary>
/// Request payload used to create a tenant gym.
/// </summary>
public sealed class CreateGymRequest
{
    /// <summary>
    /// Tenant display name.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;
}
