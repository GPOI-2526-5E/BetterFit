using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Auth;

public sealed class GetEmailVerificationSessionStatusRequest
{
    [Required]
    [MaxLength(128)]
    public string VerificationToken { get; init; } = string.Empty;
}
