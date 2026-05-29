using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Gyms;
using Betterfit.Data;
using Betterfit.Infrastructure.Mapping;
using Betterfit.Models;
using Betterfit.Services.Gyms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms")]
public class GymsController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGymProvisioningService _gymProvisioningService;

    public GymsController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        IGymProvisioningService gymProvisioningService)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
        _gymProvisioningService = gymProvisioningService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymResponse>>>> GetGyms(CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymResponse>>();
        }

        var authorizedGymIds = await _permissionService.GetAuthorizedGymIdsAsync(
            userId,
            PermissionResources.Gyms,
            PermissionActions.Read,
            cancellationToken);

        var gyms = await _dbContext.Gyms
            .AsNoTracking()
            .Where(gym => authorizedGymIds.Contains(gym.Id))
            .OrderBy(gym => gym.Name)
            .ToListAsync(cancellationToken);

        var gymResponses = gyms
            .Select(ResponseMappers.MapGymResponse)
            .ToList();

        return Success<IReadOnlyCollection<GymResponse>>(gymResponses);
    }

    [HttpGet("{gymId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.GymsRead)]
    public async Task<ActionResult<ApiResponse<GymResponse>>> GetGymById(Guid gymId, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedError<GymResponse>();
        }

        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == gymId, cancellationToken);

        if (gym is null)
        {
            return NotFoundError<GymResponse>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            userId,
            gymId,
            PermissionResources.Gyms,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymResponse>();
        }

        return Success(ResponseMappers.MapGymResponse(gym));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GymResponse>>> CreateGym(
        [FromBody] CreateGymRequest request,
        CancellationToken cancellationToken)
    {
        var creatorUser = await _userManager.GetUserAsync(User);
        if (creatorUser is null)
        {
            return UnauthorizedError<GymResponse>();
        }

        var gymName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(gymName))
        {
            return BadRequestError<GymResponse>("invalid_gym_name", "Gym name is required.");
        }

        var gym = await _gymProvisioningService.CreateGymAsync(gymName, creatorUser, cancellationToken);
        return CreatedAt(nameof(GetGymById), new { gymId = gym.Id }, ResponseMappers.MapGymResponse(gym));
    }
}
