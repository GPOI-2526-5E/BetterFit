using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Gyms;

/// <summary>
/// Request payload used to create a claim invitation for a pending membership.
/// </summary>
public sealed class CreateGymInvitationRequest
{
    [Range(1, 168)]
    public int ExpiresInHours { get; init; } = 72;
}
