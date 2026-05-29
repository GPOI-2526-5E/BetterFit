namespace Betterfit.Services.Roles;

public interface IGymRoleBootstrapper
{
    Task SeedDefaultRolesForGymAsync(Guid gymId, string? createdByUserId, CancellationToken cancellationToken);

    Task EnsureDefaultRoleTemplatePermissionsAsync(CancellationToken cancellationToken);
}
