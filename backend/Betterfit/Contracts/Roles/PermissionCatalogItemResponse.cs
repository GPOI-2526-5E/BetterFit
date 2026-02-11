namespace Betterfit.Contracts.Roles;

/// <summary>
/// Permission catalog entry returned by catalog endpoints.
/// </summary>
/// <param name="Id">Permission identifier.</param>
/// <param name="Resource">Permission resource.</param>
/// <param name="Action">Permission action.</param>
/// <param name="Description">Optional human-readable description.</param>
public sealed record PermissionCatalogItemResponse(Guid Id, string Resource, string Action, string? Description);
