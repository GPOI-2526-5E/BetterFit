namespace Betterfit.Contracts.Gyms;

/// <summary>
/// DTO returned when a membership invitation is created.
/// </summary>
public sealed record GymInvitationResponse(
    Guid InvitationId,
    Guid GymId,
    Guid MembershipId,
    string Email,
    string Token,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc);
