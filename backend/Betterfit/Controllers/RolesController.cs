using System.Security.Claims;
using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Roles;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/roles")]
public class RolesController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;

    public RolesController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RolesRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleResponse>>>> GetRoles(Guid gymId, CancellationToken cancellationToken)
    {
        if (!await GymExists(gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<RoleResponse>>("Gym not found.");
        }

        var roles = await _dbContext.GymRoles
            .AsNoTracking()
            .Where(role => role.GymId == gymId)
            .Include(role => role.Permissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .OrderByDescending(role => role.IsDefault)
            .ThenBy(role => role.Name)
            .ToListAsync(cancellationToken);

        var payload = roles.Select(ToRoleResponse).ToList();
        return Success<IReadOnlyCollection<RoleResponse>>(payload);
    }

    [HttpGet("{roleId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.RolesRead)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> GetRoleById(Guid gymId, Guid roleId, CancellationToken cancellationToken)
    {
        var role = await _dbContext.GymRoles
            .AsNoTracking()
            .Where(x => x.GymId == gymId)
            .Include(x => x.Permissions)
            .ThenInclude(permission => permission.Permission)
            .SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken);

        if (role is null)
        {
            return NotFoundError<RoleResponse>("Role not found for this gym.");
        }

        return Success(ToRoleResponse(role));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RolesWrite)]
    public async Task<ActionResult<ApiResponse<RoleResponse>>> CreateCustomRole(
        Guid gymId,
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!await GymExists(gymId, cancellationToken))
        {
            return NotFoundError<RoleResponse>("Gym not found.");
        }

        var roleName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return BadRequestError<RoleResponse>("invalid_role_name", "Role name is required.");
        }

        var normalizedName = roleName.ToUpperInvariant();
        var roleAlreadyExists = await _dbContext.GymRoles.AnyAsync(
            x => x.GymId == gymId && x.NormalizedName == normalizedName,
            cancellationToken);

        if (roleAlreadyExists)
        {
            return ConflictError<RoleResponse>("A role with this name already exists for this gym.");
        }

        var requestedPermissions = request.Permissions
            .Where(permission => permission.PermissionId != Guid.Empty)
            .GroupBy(permission => permission.PermissionId)
            .Select(group => group.Last())
            .ToList();

        if (requestedPermissions.Count == 0)
        {
            return BadRequestError<RoleResponse>(
                code: "invalid_permissions",
                message: "At least one valid permissionId is required.");
        }

        var requestedPermissionIds = requestedPermissions
            .Select(permission => permission.PermissionId)
            .Distinct()
            .ToList();

        var catalogItems = await _dbContext.PermissionCatalogItems
            .Where(item => requestedPermissionIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (catalogItems.Count != requestedPermissionIds.Count)
        {
            var knownPermissionIds = catalogItems.Select(item => item.Id).ToHashSet();
            var missingPermissionIds = requestedPermissionIds
                .Where(permissionId => !knownPermissionIds.Contains(permissionId))
                .Select(permissionId => permissionId.ToString())
                .ToArray();

            return BadRequestError<RoleResponse>(
                code: "permission_catalog_missing",
                message: "One or more permissionId values do not exist in the permission catalog.",
                details: new Dictionary<string, string[]>
                {
                    ["missingPermissionIds"] = missingPermissionIds
                });
        }

        var role = new GymRole
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            Name = roleName,
            NormalizedName = normalizedName,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsDefault = false,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
        };

        role.Permissions = requestedPermissions
            .Select(permission => new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = role.Id,
                PermissionId = permission.PermissionId,
                IsAllowed = permission.IsAllowed
            })
            .ToList();

        _dbContext.GymRoles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var createdRole = await _dbContext.GymRoles
            .AsNoTracking()
            .Where(x => x.GymId == gymId)
            .Include(x => x.Permissions)
            .ThenInclude(permission => permission.Permission)
            .SingleAsync(x => x.Id == role.Id, cancellationToken);

        return CreatedAt(
            nameof(GetRoleById),
            new { gymId, roleId = createdRole.Id },
            ToRoleResponse(createdRole));
    }

    private async Task<bool> GymExists(Guid gymId, CancellationToken cancellationToken)
    {
        return await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken);
    }

    private static RoleResponse ToRoleResponse(GymRole role)
    {
        var permissions = role.Permissions
            .OrderBy(permission => permission.Permission.Resource)
            .ThenBy(permission => permission.Permission.Action)
            .Select(permission => new PermissionResponse(
                permission.PermissionId,
                permission.Permission.Resource,
                permission.Permission.Action,
                permission.Permission.Description,
                permission.IsAllowed))
            .ToList();

        return new RoleResponse(role.Id, role.GymId, role.Name, role.IsDefault, role.Description, permissions);
    }
}
