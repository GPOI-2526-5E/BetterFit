using Betterfit.Models;

namespace Betterfit.Contracts.Crm;

public sealed record GymLeadResponse(
    Guid LeadId,
    Guid GymId,
    Guid LocationId,
    string LocationName,
    Guid? OwnerAssignmentId,
    string? OwnerName,
    Guid? ConvertedMembershipId,
    string FullName,
    string? Email,
    string? PhoneNumber,
    GymLeadSource Source,
    GymLeadStage Stage,
    string? Interest,
    string? Notes,
    DateTime? LastContactedAtUtc,
    DateTime? NextFollowUpAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyCollection<GymLeadTaskResponse> Tasks);
