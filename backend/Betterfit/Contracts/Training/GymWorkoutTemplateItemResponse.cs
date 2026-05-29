namespace Betterfit.Contracts.Training;

public sealed record GymWorkoutTemplateItemResponse(
    Guid TemplateItemId,
    Guid? ExerciseId,
    string ExerciseName,
    int DayNumber,
    int SortOrder,
    string SetsPrescription,
    string RepetitionsPrescription,
    int? RestSeconds,
    string? Tempo,
    string? Notes);
