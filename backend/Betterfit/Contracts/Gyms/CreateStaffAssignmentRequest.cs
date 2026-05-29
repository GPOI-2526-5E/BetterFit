using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Gyms;

public sealed class CreateStaffAssignmentRequest
{
    public string? UserId { get; init; }

    [EmailAddress]
    public string? Email { get; init; }

    public Guid RoleId { get; init; }

    public TenantRoleAssignmentScopeType ScopeType { get; init; } = TenantRoleAssignmentScopeType.Tenant;

    public Guid? ScopeLocationId { get; init; }

    public TenantRoleAssignmentStatus? Status { get; init; }

    public StaffProfileRequest? Profile { get; init; }
}
