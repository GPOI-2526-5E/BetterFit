using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Betterfit.Authorization;

public sealed class GymPermissionAuthorizationHandler : AuthorizationHandler<GymPermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionService _permissionService;

    public GymPermissionAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        IPermissionService permissionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GymPermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        if (!TryResolveGymId(httpContext, out var gymId))
        {
            return;
        }

        var evaluation = await _permissionService.EvaluateGymPermissionAsync(
            userId,
            gymId,
            requirement.Resource,
            requirement.Action,
            httpContext.RequestAborted);

        switch (evaluation)
        {
            case PermissionEvaluationResult.Allowed:
                context.Succeed(requirement);
                break;
            case PermissionEvaluationResult.Denied:
                context.Fail();
                break;
        }
    }

    private static bool TryResolveGymId(HttpContext httpContext, out Guid gymId)
    {
        gymId = Guid.Empty;

        if (!httpContext.Request.RouteValues.TryGetValue("gymId", out var routeValue) || routeValue is null)
        {
            return false;
        }

        return Guid.TryParse(routeValue.ToString(), out gymId);
    }
}
