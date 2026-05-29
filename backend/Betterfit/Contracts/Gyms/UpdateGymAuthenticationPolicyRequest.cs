namespace Betterfit.Contracts.Gyms;

public sealed class UpdateGymAuthenticationPolicyRequest
{
    public bool RequireTwoFactorForStaff { get; init; }

    public bool RequireTwoFactorForMembers { get; init; }
}
