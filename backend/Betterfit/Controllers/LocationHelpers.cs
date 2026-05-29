using Betterfit.Contracts.Gyms;
using Betterfit.Models;

namespace Betterfit.Controllers;

/// <summary>
/// Shared helpers for creating and normalizing location-related entities.
/// </summary>
internal static class LocationHelpers
{
    public static GymLocation CreateLocationEntity(Guid gymId, CreateGymLocationRequest request, DateTime now)
    {
        return new GymLocation
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Name = request.Name.Trim(),
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            AddressLine1 = string.IsNullOrWhiteSpace(request.AddressLine1) ? null : request.AddressLine1.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim(),
            CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? null : request.CountryCode.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAtUtc = now
        };
    }

    public static string? NormalizeEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
