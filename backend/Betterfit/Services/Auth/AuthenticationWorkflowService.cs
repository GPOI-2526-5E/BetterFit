using System.Globalization;
using Betterfit.Contracts.Auth;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Services.Gyms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Betterfit.Services.Auth;

public sealed class AuthenticationWorkflowService : IAuthenticationWorkflowService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuthenticationChallengeService _authenticationChallengeService;
    private readonly IGymAuthenticationPolicyService _gymAuthenticationPolicyService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAccountSessionService _accountSessionService;
    private readonly AuthenticationFlowOptions _options;
    private readonly ILogger<AuthenticationWorkflowService> _logger;

    public AuthenticationWorkflowService(
        UserManager<ApplicationUser> userManager,
        IAuthenticationChallengeService authenticationChallengeService,
        IGymAuthenticationPolicyService gymAuthenticationPolicyService,
        IJwtTokenService jwtTokenService,
        IAccountSessionService accountSessionService,
        IOptions<AuthenticationFlowOptions> options,
        ILogger<AuthenticationWorkflowService> logger)
    {
        _userManager = userManager;
        _authenticationChallengeService = authenticationChallengeService;
        _gymAuthenticationPolicyService = gymAuthenticationPolicyService;
        _jwtTokenService = jwtTokenService;
        _accountSessionService = accountSessionService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AuthenticationOperationResult<AuthenticationFlowResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return AuthenticationOperationResult<AuthenticationFlowResponse>.Conflict(
                "account_exists",
                "An account with this email address already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim()
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(
                error => error.Code,
                error => new[] { error.Description });

            return AuthenticationOperationResult<AuthenticationFlowResponse>.BadRequest(
                "identity_error",
                "Account creation failed.",
                errors);
        }

        _logger.LogInformation("New account registered: {UserId} ({Email})", user.Id, email);

        var challenge = await _authenticationChallengeService.IssueEmailVerificationChallengeAsync(user, cancellationToken);
        return AuthenticationOperationResult<AuthenticationFlowResponse>.Success(
            new AuthenticationFlowResponse(
                AuthenticationStep.EmailVerificationRequired,
                EmailVerification: challenge));
    }

    public async Task<AuthenticationOperationResult<AuthenticationFlowResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthenticationOperationResult<AuthenticationFlowResponse>.Unauthorized(
                "invalid_credentials",
                "Invalid email or password.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return AuthenticationOperationResult<AuthenticationFlowResponse>.Unauthorized(
                "locked_out",
                "This account is temporarily locked due to repeated failed sign-in attempts.");
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                return AuthenticationOperationResult<AuthenticationFlowResponse>.Unauthorized(
                    "locked_out",
                    "This account is temporarily locked due to repeated failed sign-in attempts.");
            }

            return AuthenticationOperationResult<AuthenticationFlowResponse>.Unauthorized(
                "invalid_credentials",
                "Invalid email or password.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (!user.EmailConfirmed)
        {
            var challenge = await _authenticationChallengeService.IssueEmailVerificationChallengeAsync(user, cancellationToken);
            return AuthenticationOperationResult<AuthenticationFlowResponse>.Success(
                new AuthenticationFlowResponse(
                    AuthenticationStep.EmailVerificationRequired,
                    EmailVerification: challenge));
        }

        return await CompleteAuthenticationAfterPrimaryFactorAsync(user, cancellationToken);
    }

    public async Task<AuthenticationOperationResult<AuthenticationFlowResponse>> VerifyEmailCodeAsync(
        VerifyEmailCodeRequest request,
        CancellationToken cancellationToken)
    {
        var confirmation = await _authenticationChallengeService.ConfirmEmailAsync(
            request.VerificationToken,
            request.Code,
            cancellationToken);

        if (!confirmation.Succeeded)
        {
            return confirmation.ErrorKind switch
            {
                AuthenticationOperationErrorKind.NotFound => AuthenticationOperationResult<AuthenticationFlowResponse>.NotFound(
                    confirmation.ErrorCode ?? "email_verification_not_found",
                    confirmation.ErrorMessage ?? "Email verification session not found or expired."),
                AuthenticationOperationErrorKind.Conflict => AuthenticationOperationResult<AuthenticationFlowResponse>.Conflict(
                    confirmation.ErrorCode ?? "email_verification_conflict",
                    confirmation.ErrorMessage ?? "Email verification cannot be completed."),
                _ => AuthenticationOperationResult<AuthenticationFlowResponse>.BadRequest(
                    confirmation.ErrorCode ?? "invalid_verification_code",
                    confirmation.ErrorMessage ?? "The verification code is invalid.")
            };
        }

        return await CompleteAuthenticationAfterPrimaryFactorAsync(confirmation.Payload!, cancellationToken);
    }

    public Task<AuthenticationOperationResult<EmailVerificationChallengeResponse>> ResendEmailVerificationCodeAsync(
        ResendEmailVerificationCodeRequest request,
        CancellationToken cancellationToken)
    {
        return _authenticationChallengeService.ResendEmailVerificationCodeAsync(
            request.VerificationToken,
            cancellationToken);
    }

    public Task<AuthenticationOperationResult<EmailVerificationChallengeResponse>> GetEmailVerificationSessionStatusAsync(
        GetEmailVerificationSessionStatusRequest request,
        CancellationToken cancellationToken)
    {
        return _authenticationChallengeService.GetEmailVerificationSessionStatusAsync(
            request.VerificationToken,
            cancellationToken);
    }

    public async Task<AuthenticationOperationResult<TwoFactorSetupResponse>> BeginTwoFactorSetupAsync(
        BeginTwoFactorSetupRequest request,
        CancellationToken cancellationToken)
    {
        var challengeResult = await _authenticationChallengeService.GetValidChallengeAsync(
            request.ChallengeToken,
            AuthenticationChallengeType.TwoFactorSetup,
            cancellationToken);

        if (!challengeResult.Succeeded)
        {
            return AuthenticationOperationResult<TwoFactorSetupResponse>.NotFound(
                challengeResult.ErrorCode ?? "two_factor_setup_not_found",
                challengeResult.ErrorMessage ?? "Two-factor setup session not found or expired.");
        }

        var challenge = challengeResult.Payload!;
        var user = challenge.User;
        if (user.TwoFactorEnabled)
        {
            return AuthenticationOperationResult<TwoFactorSetupResponse>.Conflict(
                "two_factor_already_enabled",
                "Two-factor authentication is already enabled for this account.");
        }

        var sharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(sharedKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            sharedKey = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrWhiteSpace(sharedKey))
        {
            return AuthenticationOperationResult<TwoFactorSetupResponse>.Conflict(
                "two_factor_setup_failed",
                "Unable to create an authenticator key for this account.");
        }

        var response = new TwoFactorSetupResponse(
            request.ChallengeToken.Trim(),
            challenge.SessionExpiresAtUtc,
            sharedKey,
            FormatAuthenticatorKey(sharedKey),
            BuildAuthenticatorUri(user.Email ?? user.Id, sharedKey));

        return AuthenticationOperationResult<TwoFactorSetupResponse>.Success(response);
    }

    public async Task<AuthenticationOperationResult<TwoFactorSetupCompletionResponse>> EnableTwoFactorAsync(
        EnableTwoFactorRequest request,
        CancellationToken cancellationToken)
    {
        var challengeResult = await _authenticationChallengeService.GetValidChallengeAsync(
            request.ChallengeToken,
            AuthenticationChallengeType.TwoFactorSetup,
            cancellationToken);

        if (!challengeResult.Succeeded)
        {
            return AuthenticationOperationResult<TwoFactorSetupCompletionResponse>.NotFound(
                challengeResult.ErrorCode ?? "two_factor_setup_not_found",
                challengeResult.ErrorMessage ?? "Two-factor setup session not found or expired.");
        }

        var challenge = challengeResult.Payload!;
        var user = challenge.User;
        if (user.TwoFactorEnabled)
        {
            return AuthenticationOperationResult<TwoFactorSetupCompletionResponse>.Conflict(
                "two_factor_already_enabled",
                "Two-factor authentication is already enabled for this account.");
        }

        var code = NormalizeOtpCode(request.Code);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            code);

        if (!isValid)
        {
            await _authenticationChallengeService.RecordFailedAttemptAsync(challenge, cancellationToken);
            return AuthenticationOperationResult<TwoFactorSetupCompletionResponse>.BadRequest(
                "invalid_two_factor_code",
                "The authenticator code is invalid.");
        }

        var enableResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!enableResult.Succeeded)
        {
            var errors = enableResult.Errors.ToDictionary(error => error.Code, error => new[] { error.Description });
            return AuthenticationOperationResult<TwoFactorSetupCompletionResponse>.BadRequest(
                "identity_error",
                "Unable to enable two-factor authentication.",
                errors);
        }

        var recoveryCodes = ((await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, _options.RecoveryCodeCount))
                ?? Array.Empty<string>())
            .ToArray();

        await _authenticationChallengeService.ConsumeAsync(challenge, cancellationToken);
        var auth = await BuildAuthResponseAsync(user, cancellationToken);

        return AuthenticationOperationResult<TwoFactorSetupCompletionResponse>.Success(
            new TwoFactorSetupCompletionResponse(auth, recoveryCodes));
    }

    public async Task<AuthenticationOperationResult<AuthResponse>> VerifyTwoFactorAsync(
        VerifyTwoFactorCodeRequest request,
        CancellationToken cancellationToken)
    {
        var challengeResult = await _authenticationChallengeService.GetValidChallengeAsync(
            request.ChallengeToken,
            AuthenticationChallengeType.TwoFactorLogin,
            cancellationToken);

        if (!challengeResult.Succeeded)
        {
            return AuthenticationOperationResult<AuthResponse>.NotFound(
                challengeResult.ErrorCode ?? "two_factor_not_found",
                challengeResult.ErrorMessage ?? "Two-factor authentication session not found or expired.");
        }

        var challenge = challengeResult.Payload!;
        var user = challenge.User;
        if (!user.TwoFactorEnabled)
        {
            return AuthenticationOperationResult<AuthResponse>.Conflict(
                "two_factor_not_enabled",
                "Two-factor authentication is not enabled for this account.");
        }

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            NormalizeOtpCode(request.Code));

        if (!isValid)
        {
            await _authenticationChallengeService.RecordFailedAttemptAsync(challenge, cancellationToken);
            return AuthenticationOperationResult<AuthResponse>.BadRequest(
                "invalid_two_factor_code",
                "The authenticator code is invalid.");
        }

        await _authenticationChallengeService.ConsumeAsync(challenge, cancellationToken);
        return AuthenticationOperationResult<AuthResponse>.Success(
            await BuildAuthResponseAsync(user, cancellationToken));
    }

    public async Task<AuthenticationOperationResult<AuthResponse>> UseRecoveryCodeAsync(
        UseRecoveryCodeRequest request,
        CancellationToken cancellationToken)
    {
        var challengeResult = await _authenticationChallengeService.GetValidChallengeAsync(
            request.ChallengeToken,
            AuthenticationChallengeType.TwoFactorLogin,
            cancellationToken);

        if (!challengeResult.Succeeded)
        {
            return AuthenticationOperationResult<AuthResponse>.NotFound(
                challengeResult.ErrorCode ?? "two_factor_not_found",
                challengeResult.ErrorMessage ?? "Two-factor authentication session not found or expired.");
        }

        var challenge = challengeResult.Payload!;
        var user = challenge.User;
        var recoveryCode = request.RecoveryCode.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode);

        if (!result.Succeeded)
        {
            await _authenticationChallengeService.RecordFailedAttemptAsync(challenge, cancellationToken);
            return AuthenticationOperationResult<AuthResponse>.BadRequest(
                "invalid_recovery_code",
                "The recovery code is invalid.");
        }

        await _authenticationChallengeService.ConsumeAsync(challenge, cancellationToken);
        return AuthenticationOperationResult<AuthResponse>.Success(
            await BuildAuthResponseAsync(user, cancellationToken));
    }

    private async Task<AuthenticationOperationResult<AuthenticationFlowResponse>> CompleteAuthenticationAfterPrimaryFactorAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (user.TwoFactorEnabled)
        {
            var loginChallenge = await _authenticationChallengeService.IssueTwoFactorChallengeAsync(
                user,
                AuthenticationChallengeType.TwoFactorLogin,
                cancellationToken);

            return AuthenticationOperationResult<AuthenticationFlowResponse>.Success(
                new AuthenticationFlowResponse(
                    AuthenticationStep.TwoFactorRequired,
                    TwoFactor: loginChallenge));
        }

        var twoFactorRequirement = await _gymAuthenticationPolicyService.EvaluateTwoFactorRequirementAsync(
            user.Id,
            cancellationToken);

        if (twoFactorRequirement.IsRequired)
        {
            var setupChallenge = await _authenticationChallengeService.IssueTwoFactorChallengeAsync(
                user,
                AuthenticationChallengeType.TwoFactorSetup,
                cancellationToken);

            return AuthenticationOperationResult<AuthenticationFlowResponse>.Success(
                new AuthenticationFlowResponse(
                    AuthenticationStep.TwoFactorSetupRequired,
                    TwoFactor: setupChallenge));
        }

        var auth = await BuildAuthResponseAsync(user, cancellationToken);
        return AuthenticationOperationResult<AuthenticationFlowResponse>.Success(
            new AuthenticationFlowResponse(
                AuthenticationStep.Authenticated,
                Auth: auth));
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var token = _jwtTokenService.GenerateToken(user);
        var account = await _accountSessionService.BuildCurrentAccountAsync(user.Id, cancellationToken);
        return new AuthResponse(token.AccessToken, token.ExpiresAtUtc, account);
    }

    private string BuildAuthenticatorUri(string email, string sharedKey)
    {
        var escapedIssuer = Uri.EscapeDataString(_options.AuthenticatorIssuer);
        var escapedEmail = Uri.EscapeDataString(email);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"otpauth://totp/{escapedIssuer}:{escapedEmail}?secret={sharedKey}&issuer={escapedIssuer}&digits=6");
    }

    private static string FormatAuthenticatorKey(string sharedKey)
    {
        return string.Join(
            " ",
            Enumerable.Range(0, (sharedKey.Length + 3) / 4)
                .Select(index =>
                {
                    var start = index * 4;
                    var length = Math.Min(4, sharedKey.Length - start);
                    return sharedKey.Substring(start, length).ToLowerInvariant();
                }));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeOtpCode(string code)
    {
        return code.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
    }
}
