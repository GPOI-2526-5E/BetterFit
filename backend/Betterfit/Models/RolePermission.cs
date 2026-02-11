namespace Betterfit.Models;

/// <summary>
/// Single permission rule assigned to a role.
/// </summary>
public class RolePermission
{
    /// <summary>
    /// Unique identifier of this permission assignment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Role that owns this permission assignment.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Catalog permission referenced by this role assignment.
    /// </summary>
    public Guid PermissionId { get; set; }

    /// <summary>
    /// True to allow the action for this role; false to explicitly deny it.
    /// </summary>
    public bool IsAllowed { get; set; } = true;

    /// <summary>
    /// Navigation property to the role that this assignment belongs to.
    /// </summary>
    public GymRole Role { get; set; } = null!;

    /// <summary>
    /// Navigation property to the catalog permission used by this assignment.
    /// </summary>
    public PermissionCatalogItem Permission { get; set; } = null!;
}
