using Betterfit.Models;
using Betterfit.Services.Auth;
using Betterfit.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Betterfit.Tests;

public sealed class AuthenticationChallengeServiceTests
{
    [Fact]
    public async Task IssueEmailVerificationChallengeAndConfirmEmail_SucceedsWithMatchingCode()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        var now = DateTime.UtcNow;

        var user = new ApplicationUser
        {
            Id = "user-1",
            UserName = "user@example.com",
            NormalizedUserName = "USER@EXAMPLE.COM",
            Email = "user@example.com",
            NormalizedEmail = "USER@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sender = new TestEmailVerificationSender();
        var service = new AuthenticationChallengeService(
            context,
            sender,
            Options.Create(new AuthenticationFlowOptions()));

        var challenge = await service.IssueEmailVerificationChallengeAsync(user, CancellationToken.None);
        Assert.True(challenge.SessionExpiresAtUtc > challenge.CodeExpiresAtUtc);
        Assert.True(challenge.ResendAvailableAtUtc >= now);
        Assert.True(challenge.ResendAvailableAtUtc <= now.AddSeconds(35));

        var confirmation = await service.ConfirmEmailAsync(
            challenge.VerificationToken,
            sender.LastCode!,
            CancellationToken.None);

        Assert.True(confirmation.Succeeded);
        Assert.Equal(user.Id, confirmation.Payload!.Id);
        Assert.True(user.EmailConfirmed);
        Assert.Equal("user@example.com", sender.LastEmail);
    }

    [Fact]
    public async Task ResendEmailVerificationCode_WhenSuccessful_ReturnsUpdatedResendAvailability()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var user = new ApplicationUser
        {
            Id = "user-2",
            UserName = "resend@example.com",
            NormalizedUserName = "RESEND@EXAMPLE.COM",
            Email = "resend@example.com",
            NormalizedEmail = "RESEND@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sender = new TestEmailVerificationSender();
        var options = Options.Create(new AuthenticationFlowOptions
        {
            EmailVerificationResendCooldownSeconds = 0
        });
        var service = new AuthenticationChallengeService(context, sender, options);

        var challenge = await service.IssueEmailVerificationChallengeAsync(user, CancellationToken.None);
        var resendRequestedAtUtc = DateTime.UtcNow;
        var resend = await service.ResendEmailVerificationCodeAsync(challenge.VerificationToken, CancellationToken.None);

        Assert.True(resend.Succeeded);
        Assert.NotNull(resend.Payload);
        Assert.True(resend.Payload!.SessionExpiresAtUtc >= challenge.SessionExpiresAtUtc);
        Assert.True(resend.Payload.SessionExpiresAtUtc > resend.Payload.CodeExpiresAtUtc);
        Assert.True(resend.Payload!.ResendAvailableAtUtc >= resendRequestedAtUtc.AddSeconds(-1));
        Assert.True(resend.Payload.ResendAvailableAtUtc <= resend.Payload.SessionExpiresAtUtc);
        Assert.Equal(challenge.VerificationToken, resend.Payload.VerificationToken);
    }

    [Fact]
    public async Task GetEmailVerificationSessionStatus_ReturnsCurrentSessionState()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var user = new ApplicationUser
        {
            Id = "user-3",
            UserName = "status@example.com",
            NormalizedUserName = "STATUS@EXAMPLE.COM",
            Email = "status@example.com",
            NormalizedEmail = "STATUS@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthenticationChallengeService(
            context,
            new TestEmailVerificationSender(),
            Options.Create(new AuthenticationFlowOptions()));

        var issued = await service.IssueEmailVerificationChallengeAsync(user, CancellationToken.None);
        var status = await service.GetEmailVerificationSessionStatusAsync(issued.VerificationToken, CancellationToken.None);

        Assert.True(status.Succeeded);
        Assert.NotNull(status.Payload);
        Assert.Equal(issued.VerificationToken, status.Payload!.VerificationToken);
        Assert.Equal(issued.SessionExpiresAtUtc, status.Payload.SessionExpiresAtUtc);
        Assert.Equal(issued.CodeExpiresAtUtc, status.Payload.CodeExpiresAtUtc);
    }

    [Fact]
    public async Task ConfirmEmail_WhenCodeExpiredButSessionIsStillActive_ReturnsExpiredCodeAndAllowsResend()
    {
        await using var database = await SqliteTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        var user = new ApplicationUser
        {
            Id = "user-4",
            UserName = "expired@example.com",
            NormalizedUserName = "EXPIRED@EXAMPLE.COM",
            Email = "expired@example.com",
            NormalizedEmail = "EXPIRED@EXAMPLE.COM",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var sender = new TestEmailVerificationSender();
        var service = new AuthenticationChallengeService(
            context,
            sender,
            Options.Create(new AuthenticationFlowOptions
            {
                EmailVerificationResendCooldownSeconds = 0
            }));

        var issued = await service.IssueEmailVerificationChallengeAsync(user, CancellationToken.None);
        var persistedChallenge = await context.AuthenticationChallenges.SingleAsync();
        persistedChallenge.CodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();

        var confirm = await service.ConfirmEmailAsync(
            issued.VerificationToken,
            sender.LastCode!,
            CancellationToken.None);

        Assert.False(confirm.Succeeded);
        Assert.Equal("verification_code_expired", confirm.ErrorCode);

        var resend = await service.ResendEmailVerificationCodeAsync(issued.VerificationToken, CancellationToken.None);
        Assert.True(resend.Succeeded);
        Assert.NotNull(resend.Payload);
        Assert.Equal(issued.SessionExpiresAtUtc, resend.Payload!.SessionExpiresAtUtc);
        Assert.True(resend.Payload.CodeExpiresAtUtc > DateTime.UtcNow.AddMinutes(-1));
    }
}
