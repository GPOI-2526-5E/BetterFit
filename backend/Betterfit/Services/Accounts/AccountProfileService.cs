using Betterfit.Contracts.Gyms;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Services.Accounts;

public sealed class AccountProfileService : IAccountProfileService
{
    private readonly AppDbContext _dbContext;

    public AccountProfileService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MemberProfile> EnsureMemberProfileAsync(
        ApplicationUser user,
        MemberProfileRequest? request,
        GymMembership? membership,
        CancellationToken cancellationToken)
    {
        var profile = await _dbContext.MemberProfiles.SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
        var now = DateTime.UtcNow;

        if (profile is null)
        {
            profile = new MemberProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _dbContext.MemberProfiles.Add(profile);
        }

        var (firstName, lastName) = SplitFullName(user.FullName);

        profile.FirstName = request?.FirstName
            ?? profile.FirstName
            ?? membership?.PendingFirstName
            ?? firstName;
        profile.LastName = request?.LastName
            ?? profile.LastName
            ?? membership?.PendingLastName
            ?? lastName;
        profile.BirthDate = request?.BirthDate
            ?? profile.BirthDate
            ?? membership?.PendingDateOfBirth;
        profile.EmergencyContactName = request?.EmergencyContactName
            ?? profile.EmergencyContactName
            ?? membership?.PendingEmergencyContactName;
        profile.EmergencyContactPhoneNumber = request?.EmergencyContactPhoneNumber
            ?? profile.EmergencyContactPhoneNumber
            ?? membership?.PendingEmergencyContactPhoneNumber;
        profile.UpdatedAtUtc = now;

        return profile;
    }

    public async Task<StaffProfile> EnsureStaffProfileAsync(
        ApplicationUser user,
        StaffProfileRequest? request,
        CancellationToken cancellationToken,
        bool overwriteProvidedValues = false)
    {
        var profile = await _dbContext.StaffProfiles.SingleOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
        var now = DateTime.UtcNow;

        if (profile is null)
        {
            profile = new StaffProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Active = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _dbContext.StaffProfiles.Add(profile);
        }

        var requestedDisplayName = NormalizeOptional(request?.DisplayName);
        var requestedJobTitle = NormalizeOptional(request?.JobTitle);
        var requestedInternalCode = NormalizeOptional(request?.InternalCode);

        if (overwriteProvidedValues)
        {
            profile.DisplayName = requestedDisplayName ?? profile.DisplayName ?? user.FullName;
            profile.JobTitle = requestedJobTitle ?? profile.JobTitle;
            profile.InternalCode = requestedInternalCode ?? profile.InternalCode;
        }
        else
        {
            // Staff profile is global to the Betterfit account, so initial tenant onboarding
            // only fills missing values without overwriting existing account metadata.
            profile.DisplayName = requestedDisplayName ?? profile.DisplayName ?? user.FullName;
            profile.JobTitle ??= requestedJobTitle;
            profile.InternalCode ??= requestedInternalCode;
        }

        profile.Active = true;
        profile.UpdatedAtUtc = now;

        return profile;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static (string? firstName, string? lastName) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (null, null);
        }

        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => (null, null),
            1 => (parts[0], null),
            _ => (parts[0], parts[1])
        };
    }
}
