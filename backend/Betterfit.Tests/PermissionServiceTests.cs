using Betterfit.Authorization;
using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Tests;

public sealed class PermissionServiceTests
{
    [Fact]
    public async Task LocationScopedPermission_OnlyAllowsItsOwnLocations()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var gym = new Gym { Id = Guid.NewGuid(), Name = "Gym", CreatedAtUtc = DateTime.UtcNow };
        var locationA = new GymLocation { Id = Guid.NewGuid(), GymId = gym.Id, Name = "A", CreatedAtUtc = DateTime.UtcNow };
        var locationB = new GymLocation { Id = Guid.NewGuid(), GymId = gym.Id, Name = "B", CreatedAtUtc = DateTime.UtcNow };
        var user = CreateUser("coach-1", "coach@example.com");
        var staffProfile = new StaffProfile { Id = Guid.NewGuid(), UserId = user.Id, Active = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var role = new GymRole
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            Name = "Coach",
            NormalizedName = "COACH",
            CreatedAtUtc = DateTime.UtcNow
        };

        context.AddRange(gym, locationA, locationB, user, staffProfile, role);
        await context.SaveChangesAsync();

        var membersRead = await GetPermissionIdAsync(context, "members", "read");
        context.RolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            PermissionId = membersRead,
            IsAllowed = true
        });
        context.TenantRoleAssignments.Add(new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            UserId = user.Id,
            StaffProfileId = staffProfile.Id,
            RoleId = role.Id,
            ScopeType = TenantRoleAssignmentScopeType.Location,
            ScopeLocationId = locationA.Id,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var permissionService = new PermissionService(context);
        var scope = await permissionService.GetGymPermissionScopeAsync(user.Id, gym.Id, "members", "read", CancellationToken.None);

        Assert.True(scope.HasAnyAccess);
        Assert.False(scope.HasTenantWideAccess);
        Assert.True(scope.CanAccessLocation(locationA.Id));
        Assert.False(scope.CanAccessLocation(locationB.Id));
    }

    [Fact]
    public async Task TenantWideAllow_WithLocationSpecificDeny_RemovesThatLocationOnly()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var gym = new Gym { Id = Guid.NewGuid(), Name = "Gym", CreatedAtUtc = DateTime.UtcNow };
        var locationA = new GymLocation { Id = Guid.NewGuid(), GymId = gym.Id, Name = "A", CreatedAtUtc = DateTime.UtcNow };
        var locationB = new GymLocation { Id = Guid.NewGuid(), GymId = gym.Id, Name = "B", CreatedAtUtc = DateTime.UtcNow };
        var user = CreateUser("manager-1", "manager@example.com");
        var staffProfile = new StaffProfile { Id = Guid.NewGuid(), UserId = user.Id, Active = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var tenantRole = new GymRole { Id = Guid.NewGuid(), GymId = gym.Id, Name = "Manager", NormalizedName = "MANAGER", CreatedAtUtc = DateTime.UtcNow };
        var deniedRole = new GymRole { Id = Guid.NewGuid(), GymId = gym.Id, Name = "Restricted", NormalizedName = "RESTRICTED", CreatedAtUtc = DateTime.UtcNow };

        context.AddRange(gym, locationA, locationB, user, staffProfile, tenantRole, deniedRole);
        await context.SaveChangesAsync();

        var membersRead = await GetPermissionIdAsync(context, "members", "read");
        context.RolePermissions.AddRange(
            new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = tenantRole.Id,
                PermissionId = membersRead,
                IsAllowed = true
            },
            new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = deniedRole.Id,
                PermissionId = membersRead,
                IsAllowed = false
            });

        context.TenantRoleAssignments.AddRange(
            new TenantRoleAssignment
            {
                Id = Guid.NewGuid(),
                GymId = gym.Id,
                UserId = user.Id,
                StaffProfileId = staffProfile.Id,
                RoleId = tenantRole.Id,
                ScopeType = TenantRoleAssignmentScopeType.Tenant,
                Status = TenantRoleAssignmentStatus.Active,
                GrantedAtUtc = DateTime.UtcNow
            },
            new TenantRoleAssignment
            {
                Id = Guid.NewGuid(),
                GymId = gym.Id,
                UserId = user.Id,
                StaffProfileId = staffProfile.Id,
                RoleId = deniedRole.Id,
                ScopeType = TenantRoleAssignmentScopeType.Location,
                ScopeLocationId = locationB.Id,
                Status = TenantRoleAssignmentStatus.Active,
                GrantedAtUtc = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var permissionService = new PermissionService(context);
        var scope = await permissionService.GetGymPermissionScopeAsync(user.Id, gym.Id, "members", "read", CancellationToken.None);

        Assert.True(scope.HasAnyAccess);
        Assert.False(scope.HasTenantWideAccess);
        Assert.True(scope.CanAccessLocation(locationA.Id));
        Assert.False(scope.CanAccessLocation(locationB.Id));
    }

    [Fact]
    public async Task AuthorizedGymIds_IncludeGymsWithLocationScopedAccess()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var gym = new Gym { Id = Guid.NewGuid(), Name = "Gym", CreatedAtUtc = DateTime.UtcNow };
        var location = new GymLocation { Id = Guid.NewGuid(), GymId = gym.Id, Name = "Desk", CreatedAtUtc = DateTime.UtcNow };
        var user = CreateUser("reception-1", "reception@example.com");
        var staffProfile = new StaffProfile { Id = Guid.NewGuid(), UserId = user.Id, Active = true, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        var role = new GymRole { Id = Guid.NewGuid(), GymId = gym.Id, Name = "Reception", NormalizedName = "RECEPTION", CreatedAtUtc = DateTime.UtcNow };

        context.AddRange(gym, location, user, staffProfile, role);
        await context.SaveChangesAsync();

        var gymsRead = await GetPermissionIdAsync(context, "gyms", "read");
        context.RolePermissions.Add(new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            PermissionId = gymsRead,
            IsAllowed = true
        });
        context.TenantRoleAssignments.Add(new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            UserId = user.Id,
            StaffProfileId = staffProfile.Id,
            RoleId = role.Id,
            ScopeType = TenantRoleAssignmentScopeType.Location,
            ScopeLocationId = location.Id,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var permissionService = new PermissionService(context);
        var gymIds = await permissionService.GetAuthorizedGymIdsAsync(user.Id, "gyms", "read", CancellationToken.None);

        Assert.Contains(gym.Id, gymIds);
    }

    private static async Task<Guid> GetPermissionIdAsync(AppDbContext context, string resource, string action)
    {
        return await context.PermissionCatalogItems
            .Where(item => item.Resource == resource && item.Action == action)
            .Select(item => item.Id)
            .SingleAsync();
    }

    private static ApplicationUser CreateUser(string id, string email)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
    }
}
