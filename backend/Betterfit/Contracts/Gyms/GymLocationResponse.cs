namespace Betterfit.Contracts.Gyms;

/// <summary>
/// DTO describing a physical place inside a tenant.
/// </summary>
public sealed record GymLocationResponse(
    Guid Id,
    Guid GymId,
    string Name,
    string? Code,
    string? AddressLine1,
    string? City,
    string? CountryCode,
    bool IsActive,
    DateTime CreatedAtUtc);
