namespace Betterfit.Contracts.Activities;

public sealed record GymActivityTemplateResponse(
    Guid TemplateId,
    Guid GymId,
    Guid LocationId,
    string LocationName,
    Guid? InstructorAssignmentId,
    string InstructorName,
    string Name,
    string Category,
    string? Description,
    string? ColorHex,
    int Capacity,
    int DurationMinutes,
    bool RequiresBooking,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
