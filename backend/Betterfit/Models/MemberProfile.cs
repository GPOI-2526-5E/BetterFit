using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

/// <summary>
/// Global member-side profile linked to a Betterfit account.
/// </summary>
public class MemberProfile
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; set; }

    [MaxLength(100)]
    public string? LastName { get; set; }

    public DateOnly? BirthDate { get; set; }

    [MaxLength(256)]
    public string? AvatarUrl { get; set; }

    [MaxLength(100)]
    public string? EmergencyContactName { get; set; }

    [MaxLength(32)]
    public string? EmergencyContactPhoneNumber { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public ICollection<GymMembership> GymMemberships { get; set; } = new List<GymMembership>();
}
