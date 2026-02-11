using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Gyms;
using Betterfit.Data;
using Betterfit.Models;
using Betterfit.Services.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class GymsController : ApiControllerBase
{
    private const string OwnerRoleName = "OWNER";
    private const string MemberRoleName = "MEMBER";

    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGymRoleBootstrapper _gymRoleBootstrapper;

    public GymsController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        IGymRoleBootstrapper gymRoleBootstrapper)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
        _gymRoleBootstrapper = gymRoleBootstrapper;
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
            "gyms",
            "read",
            cancellationToken);

        var gyms = await _dbContext.Gyms
            .AsNoTracking()
            .Where(gym => authorizedGymIds.Contains(gym.Id))
            .OrderBy(gym => gym.Name)
            .Select(gym => new GymResponse(gym.Id, gym.Name, gym.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymResponse>>(gyms);
    }

    [HttpGet("{gymId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.GymsRead)]
    public async Task<ActionResult<ApiResponse<GymResponse>>> GetGymById(Guid gymId, CancellationToken cancellationToken)
    {
        var gym = await _dbContext.Gyms
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == gymId, cancellationToken);

        if (gym is null)
        {
            return NotFoundError<GymResponse>("Gym not found.");
        }

        return Success(new GymResponse(gym.Id, gym.Name, gym.CreatedAtUtc));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<GymResponse>>> CreateGym([FromBody] CreateGymRequest request, CancellationToken cancellationToken)
    {
        var creatorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(creatorUserId))
        {
            return UnauthorizedError<GymResponse>();
        }

        var gymName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(gymName))
        {
            return BadRequestError<GymResponse>("invalid_gym_name", "Gym name is required.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var gym = new Gym
        {
            Id = Guid.NewGuid(),
            Name = gymName,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Gyms.Add(gym);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _gymRoleBootstrapper.SeedDefaultRolesForGymAsync(gym.Id, creatorUserId, cancellationToken);

        var ownerRoleId = await GetRoleIdByNormalizedNameAsync(gym.Id, OwnerRoleName, cancellationToken);
        if (ownerRoleId is null)
        {
            return ConflictError<GymResponse>("Default Owner role is missing for the gym.");
        }

        _dbContext.GymMemberships.Add(new GymMembership
        {
            Id = Guid.NewGuid(),
            UserId = creatorUserId,
            GymId = gym.Id,
            RoleId = ownerRoleId.Value,
            AssignedAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = new GymResponse(gym.Id, gym.Name, gym.CreatedAtUtc);
        return CreatedAt(nameof(GetGymById), new { gymId = gym.Id }, response);
    }

    [HttpGet("{gymId:guid}/memberships")]
    [Authorize(Policy = AuthorizationPolicies.MembersRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymMembershipResponse>>>> GetGymMemberships(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var gymExists = await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken);
        if (!gymExists)
        {
            return NotFoundError<IReadOnlyCollection<GymMembershipResponse>>("Gym not found.");
        }

        var memberships = await _dbContext.GymMemberships
            .AsNoTracking()
            .Where(membership => membership.GymId == gymId)
            .Include(membership => membership.Gym)
            .Include(membership => membership.Role)
            .Include(membership => membership.User)
            .OrderBy(membership => membership.User.Email)
            .Select(membership => new GymMembershipResponse(
                membership.Id,
                membership.GymId,
                membership.Gym.Name,
                membership.UserId,
                membership.User.Email ?? string.Empty,
                membership.RoleId,
                membership.Role.Name,
                membership.AssignedAtUtc))
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymMembershipResponse>>(
            memberships);
    }

    [HttpPost("{gymId:guid}/assignments")]
    [Authorize(Policy = AuthorizationPolicies.MembersWrite)]
    public async Task<ActionResult<ApiResponse<GymMembershipResponse>>> AssignUserToGym(
        Guid gymId,
        [FromBody] AssignUserToGymRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RoleId == Guid.Empty)
        {
            return BadRequestError<GymMembershipResponse>("invalid_role_id", "RoleId is required.");
        }

        var gym = await _dbContext.Gyms.SingleOrDefaultAsync(x => x.Id == gymId, cancellationToken);
        if (gym is null)
        {
            return NotFoundError<GymMembershipResponse>("Gym not found.");
        }

        var role = await _dbContext.GymRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.RoleId && x.GymId == gymId, cancellationToken);

        if (role is null)
        {
            return NotFoundError<GymMembershipResponse>("Role not found for this gym.");
        }

        var user = await ResolveUser(request);
        if (user is null)
        {
            return NotFoundError<GymMembershipResponse>("User not found. Provide a valid userId or email.");
        }

        var ownerRoleId = await GetRoleIdByNormalizedNameAsync(gymId, OwnerRoleName, cancellationToken);
        if (ownerRoleId is null)
        {
            return ConflictError<GymMembershipResponse>("Gym is missing the Owner role configuration.");
        }

        var ownerMemberships = await _dbContext.GymMemberships
            .Where(membership => membership.GymId == gymId && membership.RoleId == ownerRoleId.Value)
            .ToListAsync(cancellationToken);

        if (ownerMemberships.Count > 1)
        {
            return ConflictError<GymMembershipResponse>(
                "Gym has multiple owners. Resolve owner assignments before updating memberships.");
        }

        var currentOwnerMembership = ownerMemberships.SingleOrDefault();

        if (role.Id == ownerRoleId.Value)
        {
            return await AssignOwnerRoleAsync(gym, role, user, currentOwnerMembership, cancellationToken);
        }

        if (currentOwnerMembership is null)
        {
            return ConflictError<GymMembershipResponse>("Gym cannot exist without an owner. Assign the Owner role first.");
        }

        if (currentOwnerMembership.UserId == user.Id)
        {
            return ConflictError<GymMembershipResponse>(
                "Gym cannot exist without an owner. Transfer ownership before changing this user's role.");
        }

        var membership = await _dbContext.GymMemberships
            .SingleOrDefaultAsync(
                x => x.GymId == gymId && x.UserId == user.Id,
                cancellationToken);

        if (membership is null)
        {
            membership = new GymMembership
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAtUtc = DateTime.UtcNow
            };

            _dbContext.GymMemberships.Add(membership);
        }
        else
        {
            membership.RoleId = role.Id;
            membership.AssignedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new GymMembershipResponse(
            membership.Id,
            gym.Id,
            gym.Name,
            user.Id,
            user.Email ?? string.Empty,
            role.Id,
            role.Name,
            membership.AssignedAtUtc);

        return Success(response);
    }

    private async Task<ActionResult<ApiResponse<GymMembershipResponse>>> AssignOwnerRoleAsync(
        Gym gym,
        GymRole ownerRole,
        ApplicationUser targetUser,
        GymMembership? currentOwnerMembership,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var targetMembership = await _dbContext.GymMemberships
            .SingleOrDefaultAsync(x => x.GymId == gym.Id && x.UserId == targetUser.Id, cancellationToken);

        if (currentOwnerMembership is not null && currentOwnerMembership.UserId != targetUser.Id)
        {
            var fallbackRoleId = await ResolveFallbackRoleIdForPreviousOwnerAsync(gym.Id, ownerRole.Id, cancellationToken);
            if (fallbackRoleId is null)
            {
                return ConflictError<GymMembershipResponse>(
                    "Unable to transfer ownership because no non-owner role exists in the gym.");
            }

            currentOwnerMembership.RoleId = fallbackRoleId.Value;
            currentOwnerMembership.AssignedAtUtc = DateTime.UtcNow;
        }

        if (targetMembership is null)
        {
            targetMembership = new GymMembership
            {
                Id = Guid.NewGuid(),
                GymId = gym.Id,
                UserId = targetUser.Id,
                RoleId = ownerRole.Id,
                AssignedAtUtc = DateTime.UtcNow
            };

            _dbContext.GymMemberships.Add(targetMembership);
        }
        else
        {
            targetMembership.RoleId = ownerRole.Id;
            targetMembership.AssignedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var ownerCount = await _dbContext.GymMemberships
            .CountAsync(membership => membership.GymId == gym.Id && membership.RoleId == ownerRole.Id, cancellationToken);

        if (ownerCount != 1)
        {
            return ConflictError<GymMembershipResponse>(
                "Ownership update would violate the single-owner rule for the gym.");
        }

        await transaction.CommitAsync(cancellationToken);

        var response = new GymMembershipResponse(
            targetMembership.Id,
            gym.Id,
            gym.Name,
            targetUser.Id,
            targetUser.Email ?? string.Empty,
            ownerRole.Id,
            ownerRole.Name,
            targetMembership.AssignedAtUtc);

        return Success(response);
    }

    private async Task<ApplicationUser?> ResolveUser(AssignUserToGymRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            var byId = await _userManager.FindByIdAsync(request.UserId.Trim());
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            return await _userManager.FindByEmailAsync(request.Email.Trim());
        }

        return null;
    }

    private async Task<Guid?> GetRoleIdByNormalizedNameAsync(Guid gymId, string normalizedRoleName, CancellationToken cancellationToken)
    {
        return await _dbContext.GymRoles
            .Where(role => role.GymId == gymId && role.NormalizedName == normalizedRoleName)
            .Select(role => (Guid?)role.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid?> ResolveFallbackRoleIdForPreviousOwnerAsync(
        Guid gymId,
        Guid ownerRoleId,
        CancellationToken cancellationToken)
    {
        var memberRoleId = await GetRoleIdByNormalizedNameAsync(gymId, MemberRoleName, cancellationToken);
        if (memberRoleId is not null && memberRoleId.Value != ownerRoleId)
        {
            return memberRoleId;
        }

        return await _dbContext.GymRoles
            .Where(role => role.GymId == gymId && role.Id != ownerRoleId)
            .OrderByDescending(role => role.IsDefault)
            .ThenBy(role => role.Name)
            .Select(role => (Guid?)role.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
