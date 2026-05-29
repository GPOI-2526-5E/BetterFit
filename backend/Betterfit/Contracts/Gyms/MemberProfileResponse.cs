namespace Betterfit.Contracts.Gyms;

public sealed record MemberProfileResponse(
    Guid Id,
    string UserId,
    string? FirstName,
    string? LastName,
    DateOnly? BirthDate,
    string? AvatarUrl,
    string? EmergencyContactName,
    string? EmergencyContactPhoneNumber,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
