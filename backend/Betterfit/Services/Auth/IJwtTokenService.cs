using Betterfit.Models;

namespace Betterfit.Services.Auth;

public interface IJwtTokenService
{
    TokenResult GenerateToken(ApplicationUser user);
}
