using Microsoft.AspNetCore.Authorization;

namespace Betterfit.Authorization;

public sealed class GymPermissionRequirement : IAuthorizationRequirement
{
    public GymPermissionRequirement(string resource, string action)
    {
        Resource = resource;
        Action = action;
    }

    public string Resource { get; }

    public string Action { get; }
}
