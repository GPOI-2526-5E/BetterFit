using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Auth;

/// <summary>
/// Request payload used to create a new user account.
/// </summary>
public sealed class RegisterRequest
{
    /// <summary>
    /// Email address used as login identifier.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Plain-text password for account creation.
    /// Must satisfy the active identity password policy.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Optional full name for profile display.
    /// </summary>
    [MaxLength(120)]
    public string? FullName { get; init; }
}
