using Betterfit.Models;

namespace Betterfit.Contracts.Auth;

public sealed record AuthenticationFlowResponse(
    AuthenticationStep Step,
    AuthResponse? Auth = null,
    EmailVerificationChallengeResponse? EmailVerification = null,
    TwoFactorChallengeResponse? TwoFactor = null);

public sealed record EmailVerificationChallengeResponse(
    string VerificationToken,
    DateTime SessionExpiresAtUtc,
    DateTime CodeExpiresAtUtc,
    int CodeLength,
    DateTime ResendAvailableAtUtc);

public sealed record TwoFactorChallengeResponse(
    string ChallengeToken,
    DateTime ExpiresAtUtc);

public sealed record TwoFactorSetupResponse(
    string ChallengeToken,
    DateTime ExpiresAtUtc,
    string SharedKey,
    string FormattedSharedKey,
    string AuthenticatorUri);

public sealed record TwoFactorSetupCompletionResponse(
    AuthResponse Auth,
    IReadOnlyCollection<string> RecoveryCodes);

public enum AuthenticationStep
{
    Authenticated = 0,
    EmailVerificationRequired = 1,
    TwoFactorSetupRequired = 2,
    TwoFactorRequired = 3
}
