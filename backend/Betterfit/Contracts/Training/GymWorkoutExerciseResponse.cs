namespace Betterfit.Contracts.Training;

public sealed record GymWorkoutExerciseResponse(
    Guid ExerciseId,
    Guid GymId,
    string Name,
    string? Category,
    string? MuscleGroup,
    string? Equipment,
    string? Instructions,
    string? VideoUrl,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
