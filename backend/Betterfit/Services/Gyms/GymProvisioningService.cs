using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Services.Roles;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Services.Gyms;

public sealed class GymProvisioningService : IGymProvisioningService
{
    private readonly AppDbContext _dbContext;
    private readonly IGymRoleBootstrapper _gymRoleBootstrapper;
    private readonly IAccountProfileService _accountProfileService;
    private readonly ILogger<GymProvisioningService> _logger;

    public GymProvisioningService(
        AppDbContext dbContext,
        IGymRoleBootstrapper gymRoleBootstrapper,
        IAccountProfileService accountProfileService,
        ILogger<GymProvisioningService> logger)
    {
        _dbContext = dbContext;
        _gymRoleBootstrapper = gymRoleBootstrapper;
        _accountProfileService = accountProfileService;
        _logger = logger;
    }

    public async Task<Gym> CreateGymAsync(
        string gymName,
        ApplicationUser creatorUser,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var gym = new Gym
        {
            Id = Guid.NewGuid(),
            Name = gymName,
            CreatedAtUtc = now
        };
        var authenticationPolicy = new GymAuthenticationPolicy
        {
            GymId = gym.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Gyms.Add(gym);
        _dbContext.GymAuthenticationPolicies.Add(authenticationPolicy);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _gymRoleBootstrapper.SeedDefaultRolesForGymAsync(gym.Id, creatorUser.Id, cancellationToken);

        var ownerRoleId = await _dbContext.GymRoles
            .Where(role => role.GymId == gym.Id && role.NormalizedName == DefaultGymRoleNames.Owner)
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!ownerRoleId.HasValue)
        {
            throw new InvalidOperationException("Default Owner role is missing for the new gym.");
        }

        var staffProfile = await _accountProfileService.EnsureStaffProfileAsync(creatorUser, null, cancellationToken);

        _dbContext.TenantRoleAssignments.Add(new TenantRoleAssignment
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            UserId = creatorUser.Id,
            StaffProfileId = staffProfile.Id,
            RoleId = ownerRoleId.Value,
            ScopeType = TenantRoleAssignmentScopeType.Tenant,
            IsPrimaryOwner = true,
            Status = TenantRoleAssignmentStatus.Active,
            GrantedAtUtc = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Provisioned gym {GymId} for creator {UserId} without implicit locations.",
            gym.Id,
            creatorUser.Id);

        return gym;
    }
}
