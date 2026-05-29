using System.Security.Cryptography;
using Betterfit.Contracts.Auth;
using Betterfit.Data;
using Betterfit.Infrastructure.Security;
using Betterfit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Betterfit.Services.Auth;

public sealed class AuthenticationChallengeService : IAuthenticationChallengeService
{
    private readonly AppDbContext _dbContext;
    private readonly IEmailVerificationSender _emailVerificationSender;
    private readonly AuthenticationFlowOptions _options;

    public AuthenticationChallengeService(
        AppDbContext dbContext,
        IEmailVerificationSender emailVerificationSender,
        IOptions<AuthenticationFlowOptions> options)
    {
        _dbContext = dbContext;
        _emailVerificationSender = emailVerificationSender;
        _options = options.Value;
    }

    public async Task<EmailVerificationChallengeResponse> IssueEmailVerificationChallengeAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await InvalidateActiveChallengesAsync(user.Id, AuthenticationChallengeType.EmailVerification, now, cancellationToken);

        var verificationToken = CreateOpaqueToken();
        var verificationCode = CreateNumericCode(_options.EmailVerificationCodeLength);
        var challenge = new AuthenticationChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = AuthenticationChallengeType.EmailVerification,
            TokenHash = TokenHasher.ComputeTokenHash(verificationToken),
            CodeHash = TokenHasher.ComputeTokenHash(verificationCode),
            CreatedAtUtc = now,
            SessionExpiresAtUtc = now.AddHours(_options.EmailVerificationSessionHours),
            CodeExpiresAtUtc = now.AddMinutes(_options.EmailVerificationCodeMinutes),
            LastSentAtUtc = now,
            AttemptCount = 0,
            MaxAttempts = _options.EmailVerificationMaxAttempts
        };

        _dbContext.AuthenticationChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailVerificationSender.SendVerificationCodeAsync(
            user.Email ?? string.Empty,
            verificationCode,
            challenge.CodeExpiresAtUtc!.Value,
            cancellationToken);

        return CreateEmailVerificationChallengeResponse(
            verificationToken,
            challenge.SessionExpiresAtUtc,
            challenge.CodeExpiresAtUtc.Value,
            challenge.LastSentAtUtc);
    }

    public async Task<AuthenticationOperationResult<EmailVerificationChallengeResponse>> ResendEmailVerificationCodeAsync(
        string verificationToken,
        CancellationToken cancellationToken)
    {
        var lookupToken = NormalizeToken(verificationToken);
        var challenge = await _dbContext.AuthenticationChallenges
            .Include(x => x.User)
            .SingleOrDefaultAsync(
                x => x.TokenHash == TokenHasher.ComputeTokenHash(lookupToken)
                     && x.Type == AuthenticationChallengeType.EmailVerification
                     && x.ConsumedAtUtc == null,
                cancellationToken);

        if (challenge is null || challenge.User is null || challenge.SessionExpiresAtUtc <= DateTime.UtcNow)
        {
            return AuthenticationOperationResult<EmailVerificationChallengeResponse>.NotFound(
                "email_verification_not_found",
                "Email verification session not found or expired.");
        }

        if (challenge.User.EmailConfirmed)
        {
            return AuthenticationOperationResult<EmailVerificationChallengeResponse>.Conflict(
                "email_already_verified",
                "This account email is already verified.");
        }

        var now = DateTime.UtcNow;
        if (challenge.LastSentAtUtc.AddSeconds(_options.EmailVerificationResendCooldownSeconds) > now)
        {
            var retryAvailableAtUtc = challenge.LastSentAtUtc.AddSeconds(_options.EmailVerificationResendCooldownSeconds);
            var retryAfterSeconds = Math.Max(
                1,
                (int)Math.Ceiling((retryAvailableAtUtc - now).TotalSeconds));

            return AuthenticationOperationResult<EmailVerificationChallengeResponse>.BadRequest(
                "verification_code_recently_sent",
                "A verification code was sent recently. Please wait before requesting another one.",
                new Dictionary<string, string[]>
                {
                    ["retryAfterSeconds"] = [retryAfterSeconds.ToString()],
                    ["retryAvailableAtUtc"] = [retryAvailableAtUtc.ToString("O")]
                },
                new Dictionary<string, string>
                {
                    ["Retry-After"] = retryAfterSeconds.ToString()
                });
        }

        var verificationCode = CreateNumericCode(_options.EmailVerificationCodeLength);
        challenge.CodeHash = TokenHasher.ComputeTokenHash(verificationCode);
        challenge.CodeExpiresAtUtc = now.AddMinutes(_options.EmailVerificationCodeMinutes);
        challenge.LastSentAtUtc = now;
        challenge.AttemptCount = 0;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailVerificationSender.SendVerificationCodeAsync(
            challenge.User.Email ?? string.Empty,
            verificationCode,
            challenge.CodeExpiresAtUtc!.Value,
            cancellationToken);

        return AuthenticationOperationResult<EmailVerificationChallengeResponse>.Success(
            CreateEmailVerificationChallengeResponse(
                lookupToken,
                challenge.SessionExpiresAtUtc,
                challenge.CodeExpiresAtUtc.Value,
                challenge.LastSentAtUtc));
    }

    public async Task<AuthenticationOperationResult<EmailVerificationChallengeResponse>> GetEmailVerificationSessionStatusAsync(
        string verificationToken,
        CancellationToken cancellationToken)
    {
        var lookupToken = NormalizeToken(verificationToken);
        var challenge = await _dbContext.AuthenticationChallenges
            .Include(x => x.User)
            .SingleOrDefaultAsync(
                x => x.TokenHash == TokenHasher.ComputeTokenHash(lookupToken)
                     && x.Type == AuthenticationChallengeType.EmailVerification
                     && x.ConsumedAtUtc == null,
                cancellationToken);

        if (challenge is null || challenge.User is null || challenge.SessionExpiresAtUtc <= DateTime.UtcNow)
        {
            return AuthenticationOperationResult<EmailVerificationChallengeResponse>.NotFound(
                "email_verification_not_found",
                "Email verification session not found or expired.");
        }

        if (challenge.User.EmailConfirmed)
        {
            return AuthenticationOperationResult<EmailVerificationChallengeResponse>.Conflict(
                "email_already_verified",
                "This account email is already verified.");
        }

        if (challenge.CodeExpiresAtUtc is null)
        {
            return AuthenticationOperationResult<EmailVerificationChallengeResponse>.NotFound(
                "email_verification_not_found",
                "Email verification session not found or expired.");
        }

        return AuthenticationOperationResult<EmailVerificationChallengeResponse>.Success(
            CreateEmailVerificationChallengeResponse(
                lookupToken,
                challenge.SessionExpiresAtUtc,
                challenge.CodeExpiresAtUtc.Value,
                challenge.LastSentAtUtc));
    }

    public async Task<AuthenticationOperationResult<ApplicationUser>> ConfirmEmailAsync(
        string verificationToken,
        string code,
        CancellationToken cancellationToken)
    {
        var challengeResult = await GetValidChallengeAsync(
            verificationToken,
            AuthenticationChallengeType.EmailVerification,
            cancellationToken);

        if (!challengeResult.Succeeded)
        {
            return AuthenticationOperationResult<ApplicationUser>.NotFound(
                challengeResult.ErrorCode ?? "email_verification_not_found",
                challengeResult.ErrorMessage ?? "Email verification session not found or expired.");
        }

        var challenge = challengeResult.Payload!;
        var user = challenge.User;
        if (user.EmailConfirmed)
        {
            challenge.ConsumedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return AuthenticationOperationResult<ApplicationUser>.Success(user);
        }

        if (challenge.CodeExpiresAtUtc is null || challenge.CodeExpiresAtUtc <= DateTime.UtcNow)
        {
            return AuthenticationOperationResult<ApplicationUser>.BadRequest(
                "verification_code_expired",
                "The verification code has expired. Request a new one to continue.");
        }

        var normalizedCode = NormalizeCode(code);
        if (!string.Equals(challenge.CodeHash, TokenHasher.ComputeTokenHash(normalizedCode), StringComparison.Ordinal))
        {
            await RecordFailedAttemptAsync(challenge, cancellationToken);
            return AuthenticationOperationResult<ApplicationUser>.BadRequest(
                "invalid_verification_code",
                "The verification code is invalid.");
        }

        user.EmailConfirmed = true;
        challenge.ConsumedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return AuthenticationOperationResult<ApplicationUser>.Success(user);
    }

    public async Task<TwoFactorChallengeResponse> IssueTwoFactorChallengeAsync(
        ApplicationUser user,
        AuthenticationChallengeType type,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await InvalidateActiveChallengesAsync(user.Id, type, now, cancellationToken);

        var challengeToken = CreateOpaqueToken();
        var challenge = new AuthenticationChallenge
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Type = type,
            TokenHash = TokenHasher.ComputeTokenHash(challengeToken),
            CreatedAtUtc = now,
            SessionExpiresAtUtc = now.AddMinutes(_options.TwoFactorChallengeMinutes),
            CodeExpiresAtUtc = null,
            LastSentAtUtc = now,
            AttemptCount = 0,
            MaxAttempts = _options.TwoFactorMaxAttempts
        };

        _dbContext.AuthenticationChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TwoFactorChallengeResponse(challengeToken, challenge.SessionExpiresAtUtc);
    }

    public async Task<AuthenticationOperationResult<AuthenticationChallenge>> GetValidChallengeAsync(
        string token,
        AuthenticationChallengeType expectedType,
        CancellationToken cancellationToken)
    {
        var lookupToken = NormalizeToken(token);
        var challenge = await _dbContext.AuthenticationChallenges
            .Include(x => x.User)
            .SingleOrDefaultAsync(
                x => x.TokenHash == TokenHasher.ComputeTokenHash(lookupToken)
                     && x.Type == expectedType
                     && x.ConsumedAtUtc == null,
                cancellationToken);

        if (challenge is null || challenge.User is null || challenge.SessionExpiresAtUtc <= DateTime.UtcNow)
        {
            return AuthenticationOperationResult<AuthenticationChallenge>.NotFound(
                "authentication_challenge_not_found",
                "Authentication session not found or expired.");
        }

        return AuthenticationOperationResult<AuthenticationChallenge>.Success(challenge);
    }

    public async Task RecordFailedAttemptAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        challenge.AttemptCount += 1;
        if (challenge.AttemptCount >= challenge.MaxAttempts)
        {
            challenge.ConsumedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ConsumeAsync(AuthenticationChallenge challenge, CancellationToken cancellationToken)
    {
        challenge.ConsumedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task InvalidateActiveChallengesAsync(
        string userId,
        AuthenticationChallengeType type,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var activeChallenges = await _dbContext.AuthenticationChallenges
            .Where(x => x.UserId == userId && x.Type == type && x.ConsumedAtUtc == null)
            .ToListAsync(cancellationToken);

        if (activeChallenges.Count == 0)
        {
            return;
        }

        foreach (var activeChallenge in activeChallenges)
        {
            activeChallenge.ConsumedAtUtc = now;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string CreateOpaqueToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }

    private EmailVerificationChallengeResponse CreateEmailVerificationChallengeResponse(
        string verificationToken,
        DateTime sessionExpiresAtUtc,
        DateTime codeExpiresAtUtc,
        DateTime lastSentAtUtc)
    {
        return new EmailVerificationChallengeResponse(
            verificationToken,
            sessionExpiresAtUtc,
            codeExpiresAtUtc,
            _options.EmailVerificationCodeLength,
            lastSentAtUtc.AddSeconds(_options.EmailVerificationResendCooldownSeconds));
    }

    private static string CreateNumericCode(int codeLength)
    {
        var maxValue = (int)Math.Pow(10, codeLength);
        return RandomNumberGenerator.GetInt32(0, maxValue).ToString($"D{codeLength}");
    }

    private static string NormalizeToken(string token)
    {
        return token.Trim();
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
