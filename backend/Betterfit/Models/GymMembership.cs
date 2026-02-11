namespace Betterfit.Models;

/// <summary>
/// Assignment of a user to a gym using a specific role.
/// </summary>
public class GymMembership
{
    /// <summary>
    /// Unique identifier of the membership assignment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the assigned user.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier of the gym where the user is assigned.
    /// </summary>
    public Guid GymId { get; set; }

    /// <summary>
    /// Identifier of the role applied to the user in this gym.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// UTC timestamp when the assignment was created or last updated.
    /// </summary>
    public DateTime AssignedAtUtc { get; set; }

    /// <summary>
    /// Navigation property to the assigned user.
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Navigation property to the assigned gym.
    /// </summary>
    public Gym Gym { get; set; } = null!;

    /// <summary>
    /// Navigation property to the assigned role.
    /// </summary>
    public GymRole Role { get; set; } = null!;
}
