namespace Betterfit.Services.Auth;

public interface IEmailVerificationSender
{
    Task SendVerificationCodeAsync(
        string email,
        string code,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken);
}
