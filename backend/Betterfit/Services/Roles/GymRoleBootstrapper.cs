using Betterfit.Data;
using Betterfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Services.Roles;

public sealed class GymRoleBootstrapper : IGymRoleBootstrapper
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<GymRoleBootstrapper> _logger;

    public GymRoleBootstrapper(AppDbContext dbContext, ILogger<GymRoleBootstrapper> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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

        foreach (var template in PermissionCatalogSeed.GetDefaultRoleTemplates())
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
        _logger.LogInformation("Seeded {RoleCount} default roles for gym {GymId}", roles.Count, gymId);
    }

    public async Task EnsureDefaultRoleTemplatePermissionsAsync(CancellationToken cancellationToken)
    {
        var templates = PermissionCatalogSeed.GetDefaultRoleTemplates()
            .ToDictionary(template => template.Name.ToUpperInvariant());

            var catalogLookup = await _dbContext.PermissionCatalogItems
            .AsNoTracking()
            .ToDictionaryAsync(
                item => $"{item.NormalizedResource}:{item.NormalizedAction}",
                item => item.Id,
                cancellationToken);

        var roles = await _dbContext.GymRoles
            .AsNoTracking()
            .Where(role => role.IsDefault && templates.Keys.Contains(role.NormalizedName))
            .Select(role => new { role.Id, role.NormalizedName })
            .ToListAsync(cancellationToken);

        var roleIds = roles
            .Select(role => role.Id)
            .ToList();

        if (roleIds.Count == 0)
        {
            return;
        }

        var existingPermissionsByRole = await _dbContext.RolePermissions
            .AsNoTracking()
            .Where(permission => roleIds.Contains(permission.RoleId))
            .GroupBy(permission => permission.RoleId)
            .ToDictionaryAsync(
                group => group.Key,
                group => group.Select(permission => permission.PermissionId).ToHashSet(),
                cancellationToken);

        var missingPermissions = new List<(Guid RoleId, Guid PermissionId)>();
        foreach (var role in roles)
        {
            var template = templates[role.NormalizedName];
            var existingPermissionIds = existingPermissionsByRole.TryGetValue(role.Id, out var permissionIds)
                ? permissionIds
                : [];

            foreach (var permission in template.Permissions)
            {
                var key = $"{permission.Resource.ToUpperInvariant()}:{permission.Action.ToUpperInvariant()}";
                if (!catalogLookup.TryGetValue(key, out var permissionId) || existingPermissionIds.Contains(permissionId))
                {
                    continue;
                }

                missingPermissions.Add((role.Id, permissionId));
                existingPermissionIds.Add(permissionId);
            }
        }

        if (missingPermissions.Count == 0)
        {
            return;
        }

        var insertedPermissions = 0;
        foreach (var missingPermission in missingPermissions)
        {
            insertedPermissions += await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""RolePermissions"" (""Id"", ""RoleId"", ""PermissionId"", ""IsAllowed"")
                VALUES ({Guid.NewGuid()}, {missingPermission.RoleId}, {missingPermission.PermissionId}, {true})
                ON CONFLICT (""RoleId"", ""PermissionId"") DO NOTHING;",
                cancellationToken);
        }

        if (insertedPermissions > 0)
        {
            _logger.LogInformation(
                "Backfilled {PermissionCount} missing permissions onto default gym roles.",
                insertedPermissions);
        }
    }
}
