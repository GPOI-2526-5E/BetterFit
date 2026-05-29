using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed record GymLeadTaskResponse(
    Guid TaskId,
    Guid LeadId,
    Guid GymId,
    Guid? AssignedAssignmentId,
    string? AssignedStaffName,
    string Title,
    string? Notes,
    GymLeadTaskStatus Status,
    DateTime? DueAtUtc,
    DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
