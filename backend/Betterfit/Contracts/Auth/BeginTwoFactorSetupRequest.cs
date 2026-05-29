using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Auth;

public sealed class BeginTwoFactorSetupRequest
{
    [Required]
    [MaxLength(128)]
    public string ChallengeToken { get; init; } = string.Empty;
}
