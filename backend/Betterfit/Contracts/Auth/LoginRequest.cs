using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Auth;

/// <summary>
/// Request payload used to authenticate an existing user.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Registered email address of the user.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Plain-text password for authentication.
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}
