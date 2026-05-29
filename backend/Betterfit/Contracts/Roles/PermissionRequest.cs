namespace Betterfit.Contracts.Roles;

/// <summary>
/// Role permission assignment used when creating a custom role.
/// </summary>
public sealed class PermissionRequest
{
    /// <summary>
    /// Identifier of the permission catalog entry to assign to the role.
    /// </summary>
    public Guid PermissionId { get; init; }

    /// <summary>
    /// True to allow this permission for the role, false to deny it.
    /// </summary>
    public bool IsAllowed { get; init; } = true;
}
