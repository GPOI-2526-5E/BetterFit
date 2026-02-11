using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Roles;

/// <summary>
/// Request payload used to create a custom role in a specific gym.
/// </summary>
public class CreateRoleRequest
{
    /// <summary>
    /// Role name. Must be unique within the gym.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional role description for admins.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; init; }

    /// <summary>
    /// Permission assignments attached to the role.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<PermissionRequest> Permissions { get; init; } = [];
}
