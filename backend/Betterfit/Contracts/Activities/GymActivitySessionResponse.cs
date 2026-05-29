using Betterfit.Models;

namespace Betterfit.Contracts.Activities;

public sealed record GymActivitySessionResponse(
    Guid SessionId,
    Guid TemplateId,
    Guid GymId,
    Guid LocationId,
    string LocationName,
    Guid? InstructorAssignmentId,
    string InstructorName,
    string Title,
    string Category,
    string? ColorHex,
    int Capacity,
    int ActiveBookingsCount,
    int CheckedInCount,
    int RemainingSpots,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    GymActivitySessionStatus Status,
    string? Notes,
    IReadOnlyCollection<GymActivityBookingResponse> Bookings);
