namespace Betterfit.Services.Auth;

public sealed class UnconfiguredEmailVerificationSender : IEmailVerificationSender
{
    public Task SendVerificationCodeAsync(
        string email,
        string code,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "No email verification sender is configured. Register a production IEmailVerificationSender implementation.");
    }
}
