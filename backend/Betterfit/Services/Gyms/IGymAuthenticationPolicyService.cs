using Betterfit.Models;

namespace Betterfit.Services.Gyms;

public interface IGymAuthenticationPolicyService
{
    Task<GymAuthenticationPolicy?> GetPolicyAsync(Guid gymId, CancellationToken cancellationToken);

    Task<GymAuthenticationPolicy> UpsertPolicyAsync(
        Guid gymId,
        bool requireTwoFactorForStaff,
        bool requireTwoFactorForMembers,
        CancellationToken cancellationToken);

    Task<TwoFactorPolicyEvaluation> EvaluateTwoFactorRequirementAsync(
        string userId,
        CancellationToken cancellationToken);
}

public sealed record TwoFactorPolicyEvaluation(
    bool IsRequired,
    bool RequiredByStaffPolicy,
    bool RequiredByMemberPolicy);
