using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

/// <summary>
/// Role definition used when assigning users to a specific gym.
/// </summary>
public class GymRole
{
    /// <summary>
    /// Unique identifier of the role.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gym that owns this role.
    /// </summary>
    public Guid GymId { get; set; }

    /// <summary>
    /// Display name of the role (for example, Owner, Trainer, Front Desk).
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Case-normalized role name used for uniqueness checks within a gym.
    /// </summary>
    [MaxLength(100)]
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Optional human-readable description of the role.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether this role is one of the default roles created for a gym.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// UTC timestamp when the role was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Identifier of the user that created the role. Null for auto-seeded default roles.
    /// </summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the gym that owns this role.
    /// </summary>
    public Gym Gym { get; set; } = null!;

    /// <summary>
    /// Permission assignments attached to this role.
    /// </summary>
    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();

    /// <summary>
    /// Gym memberships currently using this role.
    /// </summary>
    public ICollection<GymMembership> GymMemberships { get; set; } = new List<GymMembership>();
}
