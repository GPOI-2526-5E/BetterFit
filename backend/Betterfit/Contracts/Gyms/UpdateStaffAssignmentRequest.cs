using Betterfit.Models;

namespace Betterfit.Contracts.Gyms;

public sealed class UpdateStaffAssignmentRequest
{
    public Guid RoleId { get; init; }

    public TenantRoleAssignmentScopeType ScopeType { get; init; } = TenantRoleAssignmentScopeType.Tenant;

    public Guid? ScopeLocationId { get; init; }

    public TenantRoleAssignmentStatus Status { get; init; } = TenantRoleAssignmentStatus.Active;

    public StaffProfileRequest? Profile { get; init; }
}
