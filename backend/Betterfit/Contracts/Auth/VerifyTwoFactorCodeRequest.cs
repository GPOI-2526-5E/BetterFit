using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Auth;

public sealed class VerifyTwoFactorCodeRequest
{
    [Required]
    [MaxLength(128)]
    public string ChallengeToken { get; init; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string Code { get; init; } = string.Empty;
}
