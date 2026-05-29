using Betterfit.Models;

namespace Betterfit.Contracts.Access;

public sealed record GymAccessEventResponse(
    Guid EventId,
    Guid GymId,
    Guid MembershipId,
    Guid LocationId,
    string MemberName,
    string MemberEmail,
    string LocationName,
    string GateName,
    GymAccessEventResult Result,
    GymAccessOrigin Origin,
    string? Reason,
    DateTime OccurredAtUtc);
