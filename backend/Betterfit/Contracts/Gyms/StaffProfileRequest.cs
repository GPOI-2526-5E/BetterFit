using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Gyms;

public sealed class StaffProfileRequest
{
    [MaxLength(120)]
    public string? DisplayName { get; init; }

    [MaxLength(120)]
    public string? JobTitle { get; init; }

    [MaxLength(64)]
    public string? InternalCode { get; init; }
}
