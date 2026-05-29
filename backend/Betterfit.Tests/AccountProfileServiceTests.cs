using Betterfit.Contracts.Gyms;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Tests.TestInfrastructure;

namespace Betterfit.Tests;

public sealed class AccountProfileServiceTests
{
    [Fact]
    public async Task EnsureStaffProfileAsync_DoesNotOverwriteExistingEmploymentFields()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var user = new ApplicationUser
        {
            Id = "coach-1",
            UserName = "coach@example.com",
            NormalizedUserName = "COACH@EXAMPLE.COM",
            Email = "coach@example.com",
            NormalizedEmail = "COACH@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };
        var existingProfile = new StaffProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            DisplayName = "Coach",
            JobTitle = "Senior Coach",
            InternalCode = "A-01",
            Active = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        context.AddRange(user, existingProfile);
        await context.SaveChangesAsync();

        var service = new AccountProfileService(context);
        var updated = await service.EnsureStaffProfileAsync(
            user,
            new StaffProfileRequest
            {
                DisplayName = "Coach Updated",
                JobTitle = "Reception",
                InternalCode = "B-99"
            },
            CancellationToken.None);

        Assert.Equal("Coach Updated", updated.DisplayName);
        Assert.Equal("Senior Coach", updated.JobTitle);
        Assert.Equal("A-01", updated.InternalCode);
    }
}
