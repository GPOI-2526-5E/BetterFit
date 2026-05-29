namespace Betterfit.Contracts.Training;

public sealed record GymTrainingOverviewResponse(
    int ExercisesCount,
    int TemplatesCount,
    int ActiveAssignmentsCount,
    int RevisionsDueCount,
    IReadOnlyCollection<GymWorkoutAssignmentResponse> RecentAssignments,
    DateTime GeneratedAtUtc);
