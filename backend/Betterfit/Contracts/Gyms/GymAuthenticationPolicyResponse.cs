namespace Betterfit.Contracts.Gyms;

public sealed record GymAuthenticationPolicyResponse(
    Guid GymId,
    bool RequireTwoFactorForStaff,
    bool RequireTwoFactorForMembers,
    DateTime UpdatedAtUtc);
