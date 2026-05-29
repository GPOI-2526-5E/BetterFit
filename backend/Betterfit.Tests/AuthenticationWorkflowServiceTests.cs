using Betterfit.Contracts.Auth;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Services.Auth;
using Betterfit.Services.Gyms;
using Betterfit.Tests.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Betterfit.Tests;

public sealed class AuthenticationWorkflowServiceTests
{
    [Fact]
    public async Task RegisterThenVerifyEmail_IssuesAuthenticatedResponse()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var emailSender = new TestEmailVerificationSender();
        var authFlowOptions = Options.Create(new AuthenticationFlowOptions());
        var challengeService = new AuthenticationChallengeService(context, emailSender, authFlowOptions);
        var workflow = new AuthenticationWorkflowService(
            IdentityTestFactory.CreateUserManager(context),
            challengeService,
            new GymAuthenticationPolicyService(context),
            new JwtTokenService(Options.Create(new JwtOptions
            {
                Issuer = "Betterfit.Test",
                Audience = "Betterfit.Test.Client",
                Key = "betterfit_test_super_secret_key_2026!",
                ExpiresMinutes = 60
            })),
            new AccountSessionService(context),
            authFlowOptions,
            NullLogger<AuthenticationWorkflowService>.Instance);

        var registerRequestedAtUtc = DateTime.UtcNow;
        var register = await workflow.RegisterAsync(
            new RegisterRequest
            {
                Email = "newuser@example.com",
                Password = "Betterfit123",
                FullName = "New User"
            },
            CancellationToken.None);

        Assert.True(register.Succeeded);
        Assert.Equal(AuthenticationStep.EmailVerificationRequired, register.Payload!.Step);
        Assert.NotNull(register.Payload.EmailVerification);
        Assert.True(register.Payload.EmailVerification!.SessionExpiresAtUtc > register.Payload.EmailVerification.CodeExpiresAtUtc);
        Assert.True(register.Payload.EmailVerification!.ResendAvailableAtUtc >= registerRequestedAtUtc);
        Assert.True(register.Payload.EmailVerification.ResendAvailableAtUtc <= registerRequestedAtUtc.AddSeconds(35));

        var verify = await workflow.VerifyEmailCodeAsync(
            new VerifyEmailCodeRequest
            {
                VerificationToken = register.Payload.EmailVerification!.VerificationToken,
                Code = emailSender.LastCode!
            },
            CancellationToken.None);

        Assert.True(verify.Succeeded);
        Assert.Equal(AuthenticationStep.Authenticated, verify.Payload!.Step);
        Assert.NotNull(verify.Payload.Auth);
        Assert.False(string.IsNullOrWhiteSpace(verify.Payload.Auth!.Token));
    }

    [Fact]
    public async Task Login_ReturnsTwoFactorSetupRequired_WhenTenantPolicyRequiresItAndUserHasNoTwoFactorYet()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var userManager = IdentityTestFactory.CreateUserManager(context);
        var user = new ApplicationUser
        {
            UserName = "member@example.com",
            Email = "member@example.com",
            FullName = "Member Example",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        var createResult = await userManager.CreateAsync(user, "Betterfit123");
        Assert.True(createResult.Succeeded);

        var gym = new Gym { Id = Guid.NewGuid(), Name = "Gym", CreatedAtUtc = DateTime.UtcNow };
        var policy = new GymAuthenticationPolicy
        {
            GymId = gym.Id,
            RequireTwoFactorForMembers = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        context.AddRange(gym, policy);
        context.GymMemberships.Add(new GymMembership
        {
            Id = Guid.NewGuid(),
            GymId = gym.Id,
            UserId = user.Id,
            Status = GymMembershipStatus.Active,
            Source = GymMembershipSource.SelfSignup,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var workflow = new AuthenticationWorkflowService(
            userManager,
            new AuthenticationChallengeService(context, new TestEmailVerificationSender(), Options.Create(new AuthenticationFlowOptions())),
            new GymAuthenticationPolicyService(context),
            new JwtTokenService(Options.Create(new JwtOptions
            {
                Issuer = "Betterfit.Test",
                Audience = "Betterfit.Test.Client",
                Key = "betterfit_test_super_secret_key_2026!",
                ExpiresMinutes = 60
            })),
            new AccountSessionService(context),
            Options.Create(new AuthenticationFlowOptions()),
            NullLogger<AuthenticationWorkflowService>.Instance);

        var login = await workflow.LoginAsync(
            new LoginRequest
            {
                Email = "member@example.com",
                Password = "Betterfit123"
            },
            CancellationToken.None);

        Assert.True(login.Succeeded);
        Assert.Equal(AuthenticationStep.TwoFactorSetupRequired, login.Payload!.Step);
        Assert.NotNull(login.Payload.TwoFactor);
    }

    [Fact]
    public async Task ResendEmailVerificationCode_WhenCooldownIsActive_ReturnsRetryTimingDetails()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var emailSender = new TestEmailVerificationSender();
        var authFlowOptions = Options.Create(new AuthenticationFlowOptions
        {
            EmailVerificationResendCooldownSeconds = 30
        });
        var challengeService = new AuthenticationChallengeService(context, emailSender, authFlowOptions);
        var workflow = new AuthenticationWorkflowService(
            IdentityTestFactory.CreateUserManager(context),
            challengeService,
            new GymAuthenticationPolicyService(context),
            new JwtTokenService(Options.Create(new JwtOptions
            {
                Issuer = "Betterfit.Test",
                Audience = "Betterfit.Test.Client",
                Key = "betterfit_test_super_secret_key_2026!",
                ExpiresMinutes = 60
            })),
            new AccountSessionService(context),
            authFlowOptions,
            NullLogger<AuthenticationWorkflowService>.Instance);

        var register = await workflow.RegisterAsync(
            new RegisterRequest
            {
                Email = "cooldown@example.com",
                Password = "Betterfit123",
                FullName = "Cooldown User"
            },
            CancellationToken.None);

        Assert.True(register.Succeeded);

        var resend = await workflow.ResendEmailVerificationCodeAsync(
            new ResendEmailVerificationCodeRequest
            {
                VerificationToken = register.Payload!.EmailVerification!.VerificationToken
            },
            CancellationToken.None);

        Assert.False(resend.Succeeded);
        Assert.Equal(AuthenticationOperationErrorKind.BadRequest, resend.ErrorKind);
        Assert.Equal("verification_code_recently_sent", resend.ErrorCode);
        Assert.NotNull(resend.Details);
        Assert.True(resend.Details.TryGetValue("retryAfterSeconds", out var retryAfterValues));
        Assert.True(int.TryParse(retryAfterValues.Single(), out var retryAfterSeconds));
        Assert.InRange(retryAfterSeconds, 1, 30);
        Assert.True(resend.Details.TryGetValue("retryAvailableAtUtc", out var retryAtValues));
        Assert.True(DateTime.TryParse(retryAtValues.Single(), out _));
        Assert.NotNull(resend.ResponseHeaders);
        Assert.True(resend.ResponseHeaders.TryGetValue("Retry-After", out var retryAfterHeader));
        Assert.Equal(retryAfterValues.Single(), retryAfterHeader);
    }
}
