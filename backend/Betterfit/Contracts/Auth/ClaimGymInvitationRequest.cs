using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Auth;

/// <summary>
/// Request payload used to claim a membership invitation.
/// </summary>
public sealed class ClaimGymInvitationRequest
{
    [Required]
    [MaxLength(256)]
    public string Token { get; init; } = string.Empty;
}
