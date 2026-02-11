using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

/// <summary>
/// Catalog entry that defines a valid permission pair (resource + action).
/// </summary>
public class PermissionCatalogItem
{
    /// <summary>
    /// Unique identifier of the catalog entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Permission resource, for example: members, workouts, billing.
    /// </summary>
    [MaxLength(100)]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// Permission action, for example: read, write, approve, export.
    /// </summary>
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Case-normalized resource used for uniqueness checks.
    /// </summary>
    [MaxLength(100)]
    public string NormalizedResource { get; set; } = string.Empty;

    /// <summary>
    /// Case-normalized action used for uniqueness checks.
    /// </summary>
    [MaxLength(100)]
    public string NormalizedAction { get; set; } = string.Empty;

    /// <summary>
    /// Optional admin-facing description of this permission.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Role permission assignments that reference this catalog entry.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
