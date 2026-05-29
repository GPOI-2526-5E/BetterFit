using Betterfit.Models;

namespace Betterfit.Services.Gyms;

public interface IGymProvisioningService
{
    Task<Gym> CreateGymAsync(
        string gymName,
        ApplicationUser creatorUser,
        CancellationToken cancellationToken);
}
