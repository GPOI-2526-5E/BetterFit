using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Gyms;

/// <summary>
/// Request payload to assign a user to a gym with a specific role.
/// </summary>
public class AssignUserToGymRequest
{
    /// <summary>
    /// Target user identifier. Optional when <see cref="Email"/> is provided.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Target user email. Optional when <see cref="UserId"/> is provided.
    /// </summary>
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>
    /// Role identifier to assign in the gym.
    /// </summary>
    public Guid RoleId { get; init; }
}
