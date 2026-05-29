using Betterfit.Services.Auth;

namespace Betterfit.Tests.TestInfrastructure;

internal sealed class TestEmailVerificationSender : IEmailVerificationSender
{
    public string? LastEmail { get; private set; }

    public string? LastCode { get; private set; }

    public DateTime? LastExpiresAtUtc { get; private set; }

    public Task SendVerificationCodeAsync(
        string email,
        string code,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        LastEmail = email;
        LastCode = code;
        LastExpiresAtUtc = expiresAtUtc;
        return Task.CompletedTask;
    }
}
