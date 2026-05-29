using Betterfit.Models;

namespace Betterfit.Contracts.Integrations;

public sealed record GymIntegrationResponse(
    Guid Id,
    Guid GymId,
    Guid? LocationId,
    string? LocationName,
    GymIntegrationType Type,
    string DisplayName,
    string ProviderName,
    GymIntegrationStatus Status,
    string? EndpointUrl,
    string? Username,
    string? ExternalAccountId,
    string? SenderIdentity,
    string? Notes,
    bool HasCredentialConfigured,
    DateTime? LastSyncAttemptAtUtc,
    bool? LastSyncSucceeded,
    string? LastSyncMessage,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
