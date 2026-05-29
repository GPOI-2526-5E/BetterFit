using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

/// <summary>
/// Secure claim invitation for linking a Betterfit account to a tenant membership.
/// </summary>
public class GymInvitation
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid GymMembershipId { get; set; }

    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public string? ClaimedByUserId { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymMembership GymMembership { get; set; } = null!;
}
