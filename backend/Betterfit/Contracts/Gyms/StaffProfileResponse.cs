namespace Betterfit.Contracts.Gyms;

public sealed record StaffProfileResponse(
    Guid Id,
    string UserId,
    string? DisplayName,
    string? JobTitle,
    string? InternalCode,
    bool Active,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
