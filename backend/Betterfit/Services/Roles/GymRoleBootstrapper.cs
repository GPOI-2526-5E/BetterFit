using Betterfit.Data;
using Betterfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Services.Roles;

public class GymRoleBootstrapper : IGymRoleBootstrapper
{
    private readonly AppDbContext _dbContext;

    public GymRoleBootstrapper(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedDefaultRolesForGymAsync(Guid gymId, string? createdByUserId, CancellationToken cancellationToken)
    {
        var gymHasRoles = await _dbContext.GymRoles.AnyAsync(role => role.GymId == gymId, cancellationToken);
        if (gymHasRoles)
        {
            return;
        }

        var catalogLookup = await _dbContext.PermissionCatalogItems
            .AsNoTracking()
            .ToDictionaryAsync(
                item => $"{item.NormalizedResource}:{item.NormalizedAction}",
                item => item.Id,
                cancellationToken);

        var now = DateTime.UtcNow;
        var roles = new List<GymRole>();

        foreach (var template in AppDbContext.GetDefaultRoleTemplates())
        {
            var role = new GymRole
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                Name = template.Name,
                NormalizedName = template.Name.ToUpperInvariant(),
                Description = template.Description,
                IsDefault = true,
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId
            };

            role.Permissions = template.Permissions
                .Select(permission =>
                {
                    var key = $"{permission.Resource.ToUpperInvariant()}:{permission.Action.ToUpperInvariant()}";
                    if (!catalogLookup.TryGetValue(key, out var permissionId))
                    {
                        throw new InvalidOperationException(
                            $"Permission catalog is missing required entry '{permission.Resource}:{permission.Action}'.");
                    }

                    return new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        IsAllowed = true
                    };
                })
                .ToList();

            roles.Add(role);
        }

        _dbContext.GymRoles.AddRange(roles);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
