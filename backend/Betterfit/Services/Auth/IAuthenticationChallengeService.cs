using Betterfit.Contracts.Auth;
using Betterfit.Models;

namespace Betterfit.Services.Auth;

public interface IAuthenticationChallengeService
{
    Task<EmailVerificationChallengeResponse> IssueEmailVerificationChallengeAsync(
        ApplicationUser user,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<EmailVerificationChallengeResponse>> ResendEmailVerificationCodeAsync(
        string verificationToken,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<EmailVerificationChallengeResponse>> GetEmailVerificationSessionStatusAsync(
        string verificationToken,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<ApplicationUser>> ConfirmEmailAsync(
        string verificationToken,
        string code,
        CancellationToken cancellationToken);

    Task<TwoFactorChallengeResponse> IssueTwoFactorChallengeAsync(
        ApplicationUser user,
        AuthenticationChallengeType type,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<AuthenticationChallenge>> GetValidChallengeAsync(
        string token,
        AuthenticationChallengeType expectedType,
        CancellationToken cancellationToken);

    Task RecordFailedAttemptAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);

    Task ConsumeAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken);
}
