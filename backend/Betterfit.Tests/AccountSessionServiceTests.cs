using Betterfit.Contracts.Auth;
using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Tests.TestInfrastructure;

namespace Betterfit.Tests;

public sealed class AccountSessionServiceTests
{
    [Fact]
    public async Task BuildCurrentAccountAsync_ReturnsLightweightSessionSummary()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@example.com",
            NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            FullName = "Alex Example",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        var memberProfile = new MemberProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FirstName = "Alex",
            LastName = "Example",
            EmergencyContactName = "Should Not Leak In Session",
            EmergencyContactPhoneNumber = "555-1111",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var staffProfile = new StaffProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DisplayName = "Coach Alex",
            InternalCode = "INT-001",
            Active = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var gym = new Gym
        {
            Id = Guid.NewGuid(),
            Name = "Downtown Club",
            CreatedAtUtc = DateTime.UtcNow
        };

        var location = new GymLocation
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            Name = "Front Desk",
            CreatedAtUtc = DateTime.UtcNow
        };

        var membership = new GymMembership
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            UserId = user.Id,
            MemberProfileId = memberProfile.Id,
            Status = GymMembershipStatus.Active,
            Source = GymMembershipSource.SelfSignup,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var role = new GymRole
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            Name = "Coach",
            NormalizedName = "COACH",
            CreatedAtUtc = DateTime.UtcNow
        };

        var assignment = new TenantRoleAssignment
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
        };

        context.AddRange(user, memberProfile, staffProfile, gym, location, membership, role, assignment);
        await context.SaveChangesAsync();

        var service = new AccountSessionService(context);

        CurrentAccountResponse session = await service.BuildCurrentAccountAsync(user.Id, CancellationToken.None);

        Assert.Equal(user.Id, session.User.Id);
        Assert.Equal(user.Email, session.User.Email);
        Assert.Equal(user.FullName, session.User.FullName);
        Assert.True(session.Access.CanAccessMemberApp);
        Assert.True(session.Access.CanAccessStaffApp);
    }
}
