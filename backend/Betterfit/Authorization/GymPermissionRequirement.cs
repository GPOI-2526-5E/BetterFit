using Microsoft.AspNetCore.Authorization;

namespace Betterfit.Authorization;

public sealed class GymPermissionRequirement : IAuthorizationRequirement
{
    public GymPermissionRequirement(
        string resource,
        string action,
        GymPermissionMinimumScope minimumScope = GymPermissionMinimumScope.AnyAssignment)
    {
        Resource = resource;
        Action = action;
        MinimumScope = minimumScope;
    }

    public string Resource { get; }

    public string Action { get; }

    public GymPermissionMinimumScope MinimumScope { get; }
}
