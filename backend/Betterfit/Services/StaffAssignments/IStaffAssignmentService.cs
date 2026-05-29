using Betterfit.Models;

namespace Betterfit.Services.StaffAssignments;

public interface IStaffAssignmentService
{
    Task<StaffAssignmentOperationResult> AssignAsync(
        Guid gymId,
        ApplicationUser user,
        StaffProfile staffProfile,
        GymRole role,
        TenantRoleAssignmentScopeType scopeType,
        Guid? scopeLocationId,
        TenantRoleAssignmentStatus requestedStatus,
        CancellationToken cancellationToken);

    Task<StaffAssignmentOperationResult> UpdateAsync(
        TenantRoleAssignment assignment,
        StaffProfile staffProfile,
        GymRole role,
        TenantRoleAssignmentScopeType scopeType,
        Guid? scopeLocationId,
        TenantRoleAssignmentStatus requestedStatus,
        CancellationToken cancellationToken);
}

public sealed record StaffAssignmentOperationResult(
    TenantRoleAssignment? Assignment,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    bool IsConflict = false)
{
    public bool Succeeded => Assignment is not null;

    public static StaffAssignmentOperationResult Conflict(string code, string message)
        => new(null, code, message, IsConflict: true);

    public static StaffAssignmentOperationResult Invalid(string code, string message)
        => new(null, code, message, IsConflict: false);

    public static StaffAssignmentOperationResult Success(TenantRoleAssignment assignment)
        => new(assignment);
}
