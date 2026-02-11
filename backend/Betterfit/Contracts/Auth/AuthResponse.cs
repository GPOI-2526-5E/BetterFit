namespace Betterfit.Contracts.Auth;

/// <summary>
/// Authentication response returned after register/login.
/// </summary>
/// <param name="Token">JWT access token.</param>
/// <param name="ExpiresAtUtc">UTC timestamp when the token expires.</param>
/// <param name="User">Basic user profile information.</param>
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserSummaryResponse User);

/// <summary>
/// Lightweight user information returned with auth responses.
/// </summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">User email address.</param>
/// <param name="FullName">Optional display name.</param>
public sealed record UserSummaryResponse(string Id, string Email, string? FullName);
