using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Auth;

public sealed class VerifyEmailCodeRequest
{
    [Required]
    [MaxLength(128)]
    public string VerificationToken { get; init; } = string.Empty;

    [Required]
    [MaxLength(16)]
    public string Code { get; init; } = string.Empty;
}
