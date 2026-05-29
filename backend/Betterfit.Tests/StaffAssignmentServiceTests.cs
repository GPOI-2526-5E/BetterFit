using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Services.StaffAssignments;
using Betterfit.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Tests;

public sealed class StaffAssignmentServiceTests
{
    [Fact]
    public async Task AssigningNewOwner_RevokesPreviousOwnerAndCreatesReplacement()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var seed = await SeedOwnerScenarioAsync(context);
        var service = new StaffAssignmentService(context);

        var result = await service.AssignAsync(
            seed.GymId,
            seed.NewOwner,
            seed.NewOwnerProfile,
            seed.OwnerRole,
            TenantRoleAssignmentScopeType.Tenant,
            scopeLocationId: null,
            TenantRoleAssignmentStatus.Active,
            CancellationToken.None);

        Assert.True(result.Succeeded);

        var assignments = await context.TenantRoleAssignments
            .Where(assignment => assignment.GymId == seed.GymId)
            .OrderBy(assignment => assignment.GrantedAtUtc)
            .ToListAsync();

        Assert.Equal(2, assignments.Count);
        Assert.Contains(assignments, assignment =>
            assignment.UserId == seed.CurrentOwner.Id
            && assignment.IsPrimaryOwner
            && assignment.Status == TenantRoleAssignmentStatus.Revoked
            && assignment.RevokedAtUtc.HasValue);
        Assert.Contains(assignments, assignment =>
            assignment.UserId == seed.NewOwner.Id
            && assignment.IsPrimaryOwner
            && assignment.Status == TenantRoleAssignmentStatus.Active
            && assignment.RevokedAtUtc is null);
    }

    [Fact]
    public async Task OwnerAssignments_MustBeTenantScoped()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var seed = await SeedOwnerScenarioAsync(context);
        var service = new StaffAssignmentService(context);

        var result = await service.AssignAsync(
            seed.GymId,
            seed.NewOwner,
            seed.NewOwnerProfile,
            seed.OwnerRole,
            TenantRoleAssignmentScopeType.Location,
            seed.LocationId,
            TenantRoleAssignmentStatus.Active,
            CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.False(result.IsConflict);
        Assert.Equal("invalid_owner_scope", result.ErrorCode);
    }

    private static async Task<(Guid GymId, Guid LocationId, GymRole OwnerRole, ApplicationUser CurrentOwner, ApplicationUser NewOwner, StaffProfile NewOwnerProfile)> SeedOwnerScenarioAsync(AppDbContext context)
    {
        var gym = new Gym { Id = Guid.NewGuid(), Name = "Gym", CreatedAtUtc = DateTime.UtcNow };
        var location = new GymLocation { Id = Guid.NewGuid(), GymId = gym.Id, Name = "Downtown", CreatedAtUtc = DateTime.UtcNow };
        var ownerRole = new GymRole
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            Name = "Owner",
            NormalizedName = "OWNER",
            CreatedAtUtc = DateTime.UtcNow
        };

        var currentOwner = CreateUser("owner-1", "owner1@example.com");
        var newOwner = CreateUser("owner-2", "owner2@example.com");
        var currentOwnerProfile = CreateStaffProfile(currentOwner.Id);
        var newOwnerProfile = CreateStaffProfile(newOwner.Id);

        context.AddRange(gym, location, ownerRole, currentOwner, newOwner, currentOwnerProfile, newOwnerProfile);
        await context.SaveChangesAsync();

        context.TenantRoleAssignments.Add(new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            UserId = currentOwner.Id,
            StaffProfileId = currentOwnerProfile.Id,
            RoleId = ownerRole.Id,
            ScopeType = TenantRoleAssignmentScopeType.Tenant,
            IsPrimaryOwner = true,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow.AddMinutes(-10)
        });
        await context.SaveChangesAsync();

        return (gym.Id, location.Id, ownerRole, currentOwner, newOwner, newOwnerProfile);
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

    private static StaffProfile CreateStaffProfile(string userId)
    {
        return new StaffProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Active = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
