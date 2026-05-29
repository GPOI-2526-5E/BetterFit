using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Gyms;
using Betterfit.Data;
using Betterfit.Infrastructure.Mapping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Betterfit.Models;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/locations")]
public sealed class LocationsController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LocationsController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.LocationsRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymLocationResponse>>>> GetLocations(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymLocationResponse>>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<GymLocationResponse>>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            userId,
            gymId,
            PermissionResources.Locations,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymLocationResponse>>();
        }

        var locations = await _dbContext.GymLocations
            .AsNoTracking()
            .Where(location => location.GymId == gymId)
            .OrderBy(location => location.Name)
            .ToListAsync(cancellationToken);

        var visibleLocations = locations
            .Where(location => scope.CanAccessLocation(location.Id))
            .Select(ResponseMappers.MapLocationResponse)
            .ToList();

        return Success<IReadOnlyCollection<GymLocationResponse>>(visibleLocations);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.LocationsWrite)]
    public async Task<ActionResult<ApiResponse<GymLocationResponse>>> CreateLocation(
        Guid gymId,
        [FromBody] CreateGymLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<GymLocationResponse>("Gym not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequestError<GymLocationResponse>("invalid_location_name", "Location name is required.");
        }

        var location = LocationHelpers.CreateLocationEntity(gymId, request, DateTime.UtcNow);
        _dbContext.GymLocations.Add(location);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAt(nameof(GetLocations), new { gymId }, ResponseMappers.MapLocationResponse(location));
    }
}
