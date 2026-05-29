namespace Betterfit.Services.Auth;

public sealed class DevelopmentEmailVerificationSender : IEmailVerificationSender
{
    private readonly ILogger<DevelopmentEmailVerificationSender> _logger;

    public DevelopmentEmailVerificationSender(ILogger<DevelopmentEmailVerificationSender> logger)
    {
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(
        string email,
        string code,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Development email verification code for {Email}: {Code} (expires {ExpiresAtUtc:o})",
            email,
            code,
            expiresAtUtc);

        return Task.CompletedTask;
    }
}
