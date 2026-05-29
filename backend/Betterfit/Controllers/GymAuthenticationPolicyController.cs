using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Gyms;
using Betterfit.Services.Gyms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/security/authentication-policy")]
public sealed class GymAuthenticationPolicyController : ApiControllerBase
{
    private readonly IGymAuthenticationPolicyService _gymAuthenticationPolicyService;

    public GymAuthenticationPolicyController(IGymAuthenticationPolicyService gymAuthenticationPolicyService)
    {
        _gymAuthenticationPolicyService = gymAuthenticationPolicyService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SecurityPolicyRead)]
    public async Task<ActionResult<ApiResponse<GymAuthenticationPolicyResponse>>> GetPolicy(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var policy = await _gymAuthenticationPolicyService.GetPolicyAsync(gymId, cancellationToken);
        if (policy is null)
        {
            return NotFoundError<GymAuthenticationPolicyResponse>("Gym not found.");
        }

        return Success(MapResponse(policy));
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.SecurityPolicyWrite)]
    public async Task<ActionResult<ApiResponse<GymAuthenticationPolicyResponse>>> UpdatePolicy(
        Guid gymId,
        [FromBody] UpdateGymAuthenticationPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var current = await _gymAuthenticationPolicyService.GetPolicyAsync(gymId, cancellationToken);
        if (current is null)
        {
            return NotFoundError<GymAuthenticationPolicyResponse>("Gym not found.");
        }

        var updated = await _gymAuthenticationPolicyService.UpsertPolicyAsync(
            gymId,
            request.RequireTwoFactorForStaff,
            request.RequireTwoFactorForMembers,
            cancellationToken);

        return Success(MapResponse(updated));
    }

    private static GymAuthenticationPolicyResponse MapResponse(Models.GymAuthenticationPolicy policy)
    {
        return new GymAuthenticationPolicyResponse(
            policy.GymId,
            policy.RequireTwoFactorForStaff,
            policy.RequireTwoFactorForMembers,
            policy.UpdatedAtUtc);
    }
}
