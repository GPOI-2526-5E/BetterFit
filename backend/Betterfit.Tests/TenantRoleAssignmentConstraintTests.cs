using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Tests;

public sealed class TenantRoleAssignmentConstraintTests
{
    [Fact]
    public async Task DuplicateTenantScopedAssignment_IsRejectedByDatabase()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var seed = await SeedAssignmentGraphAsync(context);
        context.TenantRoleAssignments.AddRange(
            CreateTenantAssignment(seed),
            CreateTenantAssignment(seed));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task DuplicateLocationScopedAssignment_IsRejectedByDatabase()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var seed = await SeedAssignmentGraphAsync(context);
        context.TenantRoleAssignments.AddRange(
            CreateLocationAssignment(seed),
            CreateLocationAssignment(seed));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task InconsistentScope_IsRejectedByCheckConstraint()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var seed = await SeedAssignmentGraphAsync(context);
        context.TenantRoleAssignments.Add(new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = seed.GymId,
            UserId = seed.UserId,
            StaffProfileId = seed.StaffProfileId,
            RoleId = seed.RoleId,
            ScopeType = TenantRoleAssignmentScopeType.Tenant,
            ScopeLocationId = seed.LocationId,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task RevokedTenantScopedAssignment_CanBeGrantedAgain()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var seed = await SeedAssignmentGraphAsync(context);
        var revokedAssignment = CreateTenantAssignment(seed);
        context.TenantRoleAssignments.Add(revokedAssignment);
        await context.SaveChangesAsync();

        revokedAssignment.Status = TenantRoleAssignmentStatus.Revoked;
        revokedAssignment.RevokedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();

        context.TenantRoleAssignments.Add(CreateTenantAssignment(seed));
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ActivePrimaryOwner_IsUniquePerGym()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var seed = await SeedAssignmentGraphAsync(context, roleName: "Owner", normalizedRoleName: "OWNER");
        var secondUser = new ApplicationUser
        {
            Id = "owner-2",
            UserName = "owner2@example.com",
            NormalizedUserName = "OWNER2@EXAMPLE.COM",
            Email = "owner2@example.com",
            NormalizedEmail = "OWNER2@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var secondStaffProfile = new StaffProfile
        {
            Id = Guid.NewGuid(),
            UserId = secondUser.Id,
            Active = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        context.AddRange(secondUser, secondStaffProfile);
        await context.SaveChangesAsync();

        context.TenantRoleAssignments.AddRange(
            CreatePrimaryOwnerAssignment(seed.GymId, seed.UserId, seed.StaffProfileId, seed.RoleId),
            CreatePrimaryOwnerAssignment(seed.GymId, secondUser.Id, secondStaffProfile.Id, seed.RoleId));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task PrimaryOwnerAssignment_MustBeTenantScoped()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var seed = await SeedAssignmentGraphAsync(context, roleName: "Owner", normalizedRoleName: "OWNER");
        context.TenantRoleAssignments.Add(new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = seed.GymId,
            UserId = seed.UserId,
            StaffProfileId = seed.StaffProfileId,
            RoleId = seed.RoleId,
            ScopeType = TenantRoleAssignmentScopeType.Location,
            ScopeLocationId = seed.LocationId,
            IsPrimaryOwner = true,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        Assert.NotNull(exception);
    }

    private static async Task<(Guid GymId, Guid LocationId, string UserId, Guid StaffProfileId, Guid RoleId)> SeedAssignmentGraphAsync(
        AppDbContext context,
        string roleName = "Manager",
        string normalizedRoleName = "MANAGER")
    {
        var gym = new Gym { Id = Guid.NewGuid(), Name = "Gym", CreatedAtUtc = DateTime.UtcNow };
        var location = new GymLocation { Id = Guid.NewGuid(), GymId = gym.Id, Name = "Desk", CreatedAtUtc = DateTime.UtcNow };
        var user = new ApplicationUser
        {
            Id = "staff-1",
            UserName = "staff@example.com",
            NormalizedUserName = "STAFF@EXAMPLE.COM",
            Email = "staff@example.com",
            NormalizedEmail = "STAFF@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var staffProfile = new StaffProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Active = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var role = new GymRole
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            Name = roleName,
            NormalizedName = normalizedRoleName,
            CreatedAtUtc = DateTime.UtcNow
        };

        context.AddRange(gym, location, user, staffProfile, role);
        await context.SaveChangesAsync();

        return (gym.Id, location.Id, user.Id, staffProfile.Id, role.Id);
    }

    private static TenantRoleAssignment CreateTenantAssignment((Guid GymId, Guid LocationId, string UserId, Guid StaffProfileId, Guid RoleId) seed)
    {
        return new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = seed.GymId,
            UserId = seed.UserId,
            StaffProfileId = seed.StaffProfileId,
            RoleId = seed.RoleId,
            ScopeType = TenantRoleAssignmentScopeType.Tenant,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow
        };
    }

    private static TenantRoleAssignment CreatePrimaryOwnerAssignment(Guid gymId, string userId, Guid staffProfileId, Guid roleId)
    {
        return new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            UserId = userId,
            StaffProfileId = staffProfileId,
            RoleId = roleId,
            ScopeType = TenantRoleAssignmentScopeType.Tenant,
            IsPrimaryOwner = true,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow
        };
    }

    private static TenantRoleAssignment CreateLocationAssignment((Guid GymId, Guid LocationId, string UserId, Guid StaffProfileId, Guid RoleId) seed)
    {
        return new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = seed.GymId,
            UserId = seed.UserId,
            StaffProfileId = seed.StaffProfileId,
            RoleId = seed.RoleId,
            ScopeType = TenantRoleAssignmentScopeType.Location,
            ScopeLocationId = seed.LocationId,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow
        };
    }
}
