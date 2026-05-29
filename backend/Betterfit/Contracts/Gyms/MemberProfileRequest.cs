using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Gyms;

public sealed class MemberProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    public DateOnly? BirthDate { get; init; }

    [MaxLength(100)]
    public string? EmergencyContactName { get; init; }

    [MaxLength(32)]
    public string? EmergencyContactPhoneNumber { get; init; }
}
