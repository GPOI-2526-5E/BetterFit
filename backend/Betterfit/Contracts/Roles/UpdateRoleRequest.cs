using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Roles;

/// <summary>
/// Request payload used to update a custom role in a specific gym.
/// </summary>
public sealed class UpdateRoleRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }

    [Required]
    [MinLength(1)]
    public List<PermissionRequest> Permissions { get; init; } = [];
}
