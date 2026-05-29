using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.Integrations;

public sealed class UpsertGymIntegrationRequest
{
    public Guid? LocationId { get; init; }

    [Required]
    [MaxLength(150)]
    public string DisplayName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ProviderName { get; init; } = string.Empty;

    public GymIntegrationStatus Status { get; init; } = GymIntegrationStatus.Draft;

    [MaxLength(512)]
    public string? EndpointUrl { get; init; }

    [MaxLength(160)]
    public string? Username { get; init; }

    [MaxLength(256)]
    public string? ApiKey { get; init; }

    [MaxLength(160)]
    public string? ExternalAccountId { get; init; }

    [MaxLength(160)]
    public string? SenderIdentity { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}
