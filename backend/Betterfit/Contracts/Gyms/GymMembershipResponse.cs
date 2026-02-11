namespace Betterfit.Contracts.Gyms;

/// <summary>
/// DTO describing a user's role assignment within a gym.
/// </summary>
/// <param name="MembershipId">Membership assignment identifier.</param>
/// <param name="GymId">Gym identifier.</param>
/// <param name="GymName">Gym display name.</param>
/// <param name="UserId">User identifier.</param>
/// <param name="UserEmail">User email.</param>
/// <param name="RoleId">Assigned role identifier.</param>
/// <param name="RoleName">Assigned role name.</param>
/// <param name="AssignedAtUtc">UTC timestamp of assignment.</param>
public sealed record GymMembershipResponse(
    Guid MembershipId,
    Guid GymId,
    string GymName,
    string UserId,
    string UserEmail,
    Guid RoleId,
    string RoleName,
    DateTime AssignedAtUtc);
