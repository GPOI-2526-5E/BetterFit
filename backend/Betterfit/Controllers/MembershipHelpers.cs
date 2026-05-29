using Betterfit.Contracts.Gyms;
using Betterfit.Models;

namespace Betterfit.Controllers;

/// <summary>
/// Shared helpers for membership entity manipulation used across controllers.
/// </summary>
internal static class MembershipHelpers
{
    public static void ClearPendingMemberFields(GymMembership membership)
    {
        membership.PendingFirstName = null;
        membership.PendingLastName = null;
        membership.PendingPhoneNumber = null;
        membership.PendingDateOfBirth = null;
        membership.PendingEmergencyContactName = null;
        membership.PendingEmergencyContactPhoneNumber = null;
    }

    public static void ApplyPendingProfile(GymMembership membership, MemberProfileRequest? profile)
    {
        if (profile is null)
        {
            return;
        }

        membership.PendingFirstName = LocationHelpers.NormalizeOptional(profile.FirstName) ?? membership.PendingFirstName;
        membership.PendingLastName = LocationHelpers.NormalizeOptional(profile.LastName) ?? membership.PendingLastName;
        membership.PendingDateOfBirth = profile.BirthDate ?? membership.PendingDateOfBirth;
        membership.PendingEmergencyContactName = LocationHelpers.NormalizeOptional(profile.EmergencyContactName) ?? membership.PendingEmergencyContactName;
        membership.PendingEmergencyContactPhoneNumber = LocationHelpers.NormalizeOptional(profile.EmergencyContactPhoneNumber) ?? membership.PendingEmergencyContactPhoneNumber;
    }

    public static void SyncMembershipLocations(
        GymMembership membership,
        IReadOnlyList<Guid> requestedLocationIds,
        DateTime now)
    {
        var existing = membership.Locations.ToDictionary(location => location.LocationId);
        var requested = new HashSet<Guid>(requestedLocationIds);

        // Remove locations no longer requested
        foreach (var item in membership.Locations.Where(location => !requested.Contains(location.LocationId)).ToList())
        {
            membership.Locations.Remove(item);
        }

        // Add newly requested locations
        foreach (var locationId in requestedLocationIds.Where(id => !existing.ContainsKey(id)))
        {
            membership.Locations.Add(new GymMembershipLocation
            {
                Id = Guid.NewGuid(),
                GymMembershipId = membership.Id,
                LocationId = locationId,
                AssignedAtUtc = now
            });
        }
    }
}
