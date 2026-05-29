using Betterfit.Data;
using Betterfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Services.Gyms;

public sealed class GymAuthenticationPolicyService : IGymAuthenticationPolicyService
{
    private readonly AppDbContext _dbContext;

    public GymAuthenticationPolicyService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GymAuthenticationPolicy?> GetPolicyAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var gymExists = await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken);
        if (!gymExists)
        {
            return null;
        }

        return await EnsurePolicyAsync(gymId, cancellationToken);
    }

    public async Task<GymAuthenticationPolicy> UpsertPolicyAsync(
        Guid gymId,
        bool requireTwoFactorForStaff,
        bool requireTwoFactorForMembers,
        CancellationToken cancellationToken)
    {
        var policy = await EnsurePolicyAsync(gymId, cancellationToken);
        policy.RequireTwoFactorForStaff = requireTwoFactorForStaff;
        policy.RequireTwoFactorForMembers = requireTwoFactorForMembers;
        policy.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<TwoFactorPolicyEvaluation> EvaluateTwoFactorRequirementAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var requiredByStaffPolicy = await (
            from assignment in _dbContext.TenantRoleAssignments.AsNoTracking()
            join policy in _dbContext.GymAuthenticationPolicies.AsNoTracking()
                on assignment.GymId equals policy.GymId
            where assignment.UserId == userId
                  && assignment.Status == TenantRoleAssignmentStatus.Active
                  && assignment.RevokedAtUtc == null
                  && policy.RequireTwoFactorForStaff
            select policy.GymId)
            .AnyAsync(cancellationToken);

        var requiredByMemberPolicy = await (
            from membership in _dbContext.GymMemberships.AsNoTracking()
            join policy in _dbContext.GymAuthenticationPolicies.AsNoTracking()
                on membership.GymId equals policy.GymId
            where membership.UserId == userId
                  && (membership.Status == GymMembershipStatus.Active || membership.Status == GymMembershipStatus.Suspended)
                  && policy.RequireTwoFactorForMembers
            select policy.GymId)
            .AnyAsync(cancellationToken);

        return new TwoFactorPolicyEvaluation(
            requiredByStaffPolicy || requiredByMemberPolicy,
            requiredByStaffPolicy,
            requiredByMemberPolicy);
    }

    private async Task<GymAuthenticationPolicy> EnsurePolicyAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var policy = await _dbContext.GymAuthenticationPolicies.SingleOrDefaultAsync(
            x => x.GymId == gymId,
            cancellationToken);

        if (policy is not null)
        {
            return policy;
        }

        var now = DateTime.UtcNow;
        policy = new GymAuthenticationPolicy
        {
            GymId = gymId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymAuthenticationPolicies.Add(policy);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return policy;
    }
}
