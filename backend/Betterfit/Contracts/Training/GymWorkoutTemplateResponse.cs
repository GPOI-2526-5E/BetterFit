using Betterfit.Models;

namespace Betterfit.Contracts.Training;

public sealed record GymWorkoutTemplateResponse(
    Guid TemplateId,
    Guid GymId,
    Guid LocationId,
    string LocationName,
    Guid? CoachAssignmentId,
    string CoachName,
    string Name,
    string? Goal,
    GymWorkoutTemplateLevel Level,
    string? Description,
    int DaysPerWeek,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<GymWorkoutTemplateItemResponse> Items);
