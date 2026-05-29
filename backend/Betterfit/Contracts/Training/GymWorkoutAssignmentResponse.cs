using Betterfit.Models;

namespace Betterfit.Contracts.Training;

public sealed record GymWorkoutAssignmentResponse(
    Guid AssignmentId,
    Guid GymId,
    Guid MembershipId,
    Guid LocationId,
    string LocationName,
    Guid? TemplateId,
    string? TemplateName,
    Guid? CoachAssignmentId,
    string CoachName,
    string MemberName,
    string MemberEmail,
    string Title,
    string? Goal,
    GymWorkoutAssignmentStatus Status,
    DateTime AssignedAtUtc,
    DateTime? StartsAtUtc,
    DateTime? RevisionDueAtUtc,
    DateTime? CompletedAtUtc,
    string? Notes,
    IReadOnlyCollection<GymWorkoutAssignmentItemResponse> Items);
