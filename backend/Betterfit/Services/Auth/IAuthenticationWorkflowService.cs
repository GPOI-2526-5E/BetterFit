using Betterfit.Contracts.Auth;

namespace Betterfit.Services.Auth;

public interface IAuthenticationWorkflowService
{
    Task<AuthenticationOperationResult<AuthenticationFlowResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<AuthenticationFlowResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<AuthenticationFlowResponse>> VerifyEmailCodeAsync(
        VerifyEmailCodeRequest request,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<EmailVerificationChallengeResponse>> ResendEmailVerificationCodeAsync(
        ResendEmailVerificationCodeRequest request,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<EmailVerificationChallengeResponse>> GetEmailVerificationSessionStatusAsync(
        GetEmailVerificationSessionStatusRequest request,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<TwoFactorSetupResponse>> BeginTwoFactorSetupAsync(
        BeginTwoFactorSetupRequest request,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<TwoFactorSetupCompletionResponse>> EnableTwoFactorAsync(
        EnableTwoFactorRequest request,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<AuthResponse>> VerifyTwoFactorAsync(
        VerifyTwoFactorCodeRequest request,
        CancellationToken cancellationToken);

    Task<AuthenticationOperationResult<AuthResponse>> UseRecoveryCodeAsync(
        UseRecoveryCodeRequest request,
        CancellationToken cancellationToken);
}
