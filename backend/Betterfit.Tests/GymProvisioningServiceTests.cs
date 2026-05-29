using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Services.Gyms;
using Betterfit.Services.Roles;
using Betterfit.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Betterfit.Tests;

public sealed class GymProvisioningServiceTests
{
    [Fact]
    public async Task CreateGymAsync_CreatesTenantWithoutImplicitLocations_AndAssignsOwner()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var creatorUser = new ApplicationUser
        {
            Id = "owner-1",
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            FullName = "Owner Example",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        context.Users.Add(creatorUser);
        await context.SaveChangesAsync();

        var service = new GymProvisioningService(
            context,
            new GymRoleBootstrapper(context, NullLogger<GymRoleBootstrapper>.Instance),
            new AccountProfileService(context),
            NullLogger<GymProvisioningService>.Instance);

        var gym = await service.CreateGymAsync("Northside Fitness", creatorUser, CancellationToken.None);

        var persistedGym = await context.Gyms.SingleAsync(x => x.Id == gym.Id);
        var locations = await context.GymLocations.Where(location => location.GymId == gym.Id).ToListAsync();
        var ownerAssignment = await context.TenantRoleAssignments
            .SingleAsync(assignment => assignment.GymId == gym.Id && assignment.UserId == creatorUser.Id);
        var ownerRole = await context.GymRoles.SingleAsync(role => role.Id == ownerAssignment.RoleId);
        var staffProfile = await context.StaffProfiles.SingleAsync(profile => profile.UserId == creatorUser.Id);
        var authenticationPolicy = await context.GymAuthenticationPolicies.SingleAsync(policy => policy.GymId == gym.Id);

        Assert.Equal("Northside Fitness", persistedGym.Name);
        Assert.Empty(locations);
        Assert.False(authenticationPolicy.RequireTwoFactorForStaff);
        Assert.False(authenticationPolicy.RequireTwoFactorForMembers);
        Assert.Equal(TenantRoleAssignmentScopeType.Tenant, ownerAssignment.ScopeType);
        Assert.True(ownerAssignment.IsPrimaryOwner);
        Assert.Equal(TenantRoleAssignmentStatus.Active, ownerAssignment.Status);
        Assert.Equal(DefaultGymRoleNames.Owner, ownerRole.NormalizedName);
        Assert.Equal(staffProfile.Id, ownerAssignment.StaffProfileId);
    }
}
