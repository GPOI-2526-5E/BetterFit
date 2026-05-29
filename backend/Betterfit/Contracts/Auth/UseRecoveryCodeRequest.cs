using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Auth;

public sealed class UseRecoveryCodeRequest
{
    [Required]
    [MaxLength(128)]
    public string ChallengeToken { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string RecoveryCode { get; init; } = string.Empty;
}
