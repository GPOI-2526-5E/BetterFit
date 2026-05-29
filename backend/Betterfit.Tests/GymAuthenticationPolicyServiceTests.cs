using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Services.Gyms;
using Betterfit.Tests.TestInfrastructure;

namespace Betterfit.Tests;

public sealed class GymAuthenticationPolicyServiceTests
{
    [Fact]
    public async Task EvaluateTwoFactorRequirementAsync_RequiresTwoFactorForActiveStaffWhenTenantPolicyDemandsIt()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var gym = new Gym { Id = Guid.NewGuid(), Name = "Gym", CreatedAtUtc = DateTime.UtcNow };
        var policy = new GymAuthenticationPolicy
        {
            GymId = gym.Id,
            RequireTwoFactorForStaff = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var user = CreateUser("staff-1", "staff@example.com");
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
            Name = "Owner",
            NormalizedName = "OWNER",
            CreatedAtUtc = DateTime.UtcNow
        };

        context.AddRange(gym, policy, user, staffProfile, role);
        context.TenantRoleAssignments.Add(new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            UserId = user.Id,
            StaffProfileId = staffProfile.Id,
            RoleId = role.Id,
            ScopeType = TenantRoleAssignmentScopeType.Tenant,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new GymAuthenticationPolicyService(context);
        var evaluation = await service.EvaluateTwoFactorRequirementAsync(user.Id, CancellationToken.None);

        Assert.True(evaluation.IsRequired);
        Assert.True(evaluation.RequiredByStaffPolicy);
        Assert.False(evaluation.RequiredByMemberPolicy);
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
