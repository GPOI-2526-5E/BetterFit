namespace Betterfit.Services.Auth;

public sealed class AuthenticationFlowOptions
{
    public const string SectionName = "AuthenticationFlow";

    public int EmailVerificationCodeLength { get; set; } = 6;

    public int EmailVerificationCodeMinutes { get; set; } = 15;

    public int EmailVerificationSessionHours { get; set; } = 24;

    public int EmailVerificationMaxAttempts { get; set; } = 5;

    public int EmailVerificationResendCooldownSeconds { get; set; } = 30;

    public int TwoFactorChallengeMinutes { get; set; } = 10;

    public int TwoFactorMaxAttempts { get; set; } = 5;

    public int RecoveryCodeCount { get; set; } = 10;

    public string AuthenticatorIssuer { get; set; } = "Betterfit";
}
