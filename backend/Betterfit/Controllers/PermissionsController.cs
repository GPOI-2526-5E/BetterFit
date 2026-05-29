using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Roles;
using Betterfit.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/permissions")]
public class PermissionsController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;

    public PermissionsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("catalog")]
    [Authorize(Policy = AuthorizationPolicies.RolesRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PermissionCatalogItemResponse>>>> GetPermissionCatalog(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var gymExists = await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken);
        if (!gymExists)
        {
            return NotFoundError<IReadOnlyCollection<PermissionCatalogItemResponse>>("Gym not found.");
        }

        var permissions = await _dbContext.PermissionCatalogItems
            .AsNoTracking()
            .OrderBy(permission => permission.Resource)
            .ThenBy(permission => permission.Action)
            .Select(permission => new PermissionCatalogItemResponse(
                permission.Id,
                permission.Resource,
                permission.Action,
                permission.DescriptionKey))
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<PermissionCatalogItemResponse>>(
            permissions);
    }
}
