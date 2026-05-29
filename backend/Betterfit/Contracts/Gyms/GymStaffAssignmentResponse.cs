using Betterfit.Models;

namespace Betterfit.Contracts.Gyms;

public sealed record GymStaffAssignmentResponse(
    Guid AssignmentId,
    Guid GymId,
    string GymName,
    string UserId,
    string UserEmail,
    Guid RoleId,
    string RoleName,
    TenantRoleAssignmentScopeType ScopeType,
    Guid? ScopeLocationId,
    string? ScopeLocationName,
    TenantRoleAssignmentStatus Status,
    DateTime GrantedAtUtc,
    DateTime? RevokedAtUtc,
    StaffProfileResponse? StaffProfile);
