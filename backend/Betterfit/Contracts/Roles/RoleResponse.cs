namespace Betterfit.Contracts.Roles;

/// <summary>
/// Role DTO returned by role endpoints.
/// </summary>
/// <param name="Id">Role identifier.</param>
/// <param name="GymId">Gym that owns the role.</param>
/// <param name="Name">Role display name.</param>
/// <param name="IsDefault">True when role is a built-in default role for the gym.</param>
/// <param name="Description">Optional role description.</param>
/// <param name="Permissions">Permission rules associated with the role.</param>
public sealed record RoleResponse(
    Guid Id,
    Guid GymId,
    string Name,
    bool IsDefault,
    string? Description,
    IReadOnlyCollection<PermissionResponse> Permissions);

/// <summary>
/// Permission DTO returned with role details.
/// </summary>
/// <param name="PermissionId">Catalog permission identifier.</param>
/// <param name="Resource">Target resource name.</param>
/// <param name="Action">Action name for the resource.</param>
/// <param name="DescriptionKey">Stable localization key from the permission catalog.</param>
/// <param name="IsAllowed">True to allow action, false to deny action.</param>
public sealed record PermissionResponse(
    Guid PermissionId,
    string Resource,
    string Action,
    string? DescriptionKey,
    bool IsAllowed);
