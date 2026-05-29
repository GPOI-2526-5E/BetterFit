using Betterfit.Models;

namespace Betterfit.Contracts.Activities;

public sealed record GymActivityBookingResponse(
    Guid BookingId,
    Guid SessionId,
    Guid MembershipId,
    string MemberName,
    string MemberEmail,
    GymActivityBookingStatus Status,
    DateTime BookedAtUtc,
    DateTime? CheckedInAtUtc,
    DateTime? CancelledAtUtc,
    string? Notes);
