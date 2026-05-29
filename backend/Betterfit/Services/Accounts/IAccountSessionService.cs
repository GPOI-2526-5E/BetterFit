using Betterfit.Contracts.Auth;

namespace Betterfit.Services.Accounts;

public interface IAccountSessionService
{
    Task<CurrentAccountResponse> BuildCurrentAccountAsync(string userId, CancellationToken cancellationToken);
}
