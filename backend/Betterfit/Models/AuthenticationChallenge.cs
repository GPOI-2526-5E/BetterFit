using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class AuthenticationChallenge
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public AuthenticationChallengeType Type { get; set; }

    [MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    [MaxLength(128)]
    public string? CodeHash { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime SessionExpiresAtUtc { get; set; }

    public DateTime? CodeExpiresAtUtc { get; set; }

    public DateTime LastSentAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
