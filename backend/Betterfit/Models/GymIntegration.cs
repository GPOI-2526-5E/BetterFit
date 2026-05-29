using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymIntegration
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid? LocationId { get; set; }

    public GymIntegrationType Type { get; set; } = GymIntegrationType.EmailDelivery;

    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ProviderName { get; set; } = string.Empty;

    public GymIntegrationStatus Status { get; set; } = GymIntegrationStatus.Draft;

    [MaxLength(512)]
    public string? EndpointUrl { get; set; }

    [MaxLength(160)]
    public string? Username { get; set; }

    [MaxLength(256)]
    public string? ApiKey { get; set; }

    [MaxLength(160)]
    public string? ExternalAccountId { get; set; }

    [MaxLength(160)]
    public string? SenderIdentity { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime? LastSyncAttemptAtUtc { get; set; }

    public bool? LastSyncSucceeded { get; set; }

    [MaxLength(500)]
    public string? LastSyncMessage { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymLocation? Location { get; set; }
}
