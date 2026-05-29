using Betterfit.Contracts.Gyms;
using Betterfit.Models;

namespace Betterfit.Services.Accounts;

public interface IAccountProfileService
{
    Task<MemberProfile> EnsureMemberProfileAsync(
        ApplicationUser user,
        MemberProfileRequest? request,
        GymMembership? membership,
        CancellationToken cancellationToken);

    Task<StaffProfile> EnsureStaffProfileAsync(
        ApplicationUser user,
        StaffProfileRequest? request,
        CancellationToken cancellationToken,
        bool overwriteProvidedValues = false);
}
