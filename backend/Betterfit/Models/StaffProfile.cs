using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

/// <summary>
/// Global staff-side profile linked to a Betterfit account.
/// </summary>
public class StaffProfile
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? DisplayName { get; set; }

    [MaxLength(120)]
    public string? JobTitle { get; set; }

    [MaxLength(64)]
    public string? InternalCode { get; set; }

    public bool Active { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public ICollection<TenantRoleAssignment> RoleAssignments { get; set; } = new List<TenantRoleAssignment>();
}
