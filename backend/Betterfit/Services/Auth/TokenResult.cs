namespace Betterfit.Services.Auth;

public sealed record TokenResult(string AccessToken, DateTime ExpiresAtUtc);
