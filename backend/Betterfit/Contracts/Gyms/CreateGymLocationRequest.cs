using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Gyms;

/// <summary>
/// Request payload used to create a physical gym location.
/// </summary>
public sealed class CreateGymLocationRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(64)]
    public string? Code { get; init; }

    [MaxLength(200)]
    public string? AddressLine1 { get; init; }

    [MaxLength(120)]
    public string? City { get; init; }

    [MaxLength(2)]
    public string? CountryCode { get; init; }
}
