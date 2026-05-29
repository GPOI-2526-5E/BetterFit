using Betterfit.Models;

namespace Betterfit.Contracts.Gyms;

public sealed record GymMembershipResponse(
    Guid MembershipId,
    Guid GymId,
    string GymName,
    string? UserId,
    string? UserEmail,
    string? InvitationEmail,
    GymMembershipStatus Status,
    Guid? PrimaryLocationId,
    GymMembershipSource Source,
    string? TaxCode,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ClaimedAtUtc,
    DateTime? JoinedAtUtc,
    DateTime? EndedAtUtc,
    IReadOnlyCollection<GymLocationResponse> Locations,
    MemberProfileResponse? MemberProfile,
    IReadOnlyCollection<GymCustomFieldValueResponse> CustomFields);
