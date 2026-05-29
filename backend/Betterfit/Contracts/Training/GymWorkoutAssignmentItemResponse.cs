namespace Betterfit.Contracts.Training;

public sealed record GymWorkoutAssignmentItemResponse(
    Guid AssignmentItemId,
    Guid? ExerciseId,
    string ExerciseName,
    int DayNumber,
    int SortOrder,
    string SetsPrescription,
    string RepetitionsPrescription,
    int? RestSeconds,
    string? Tempo,
    string? Notes);
