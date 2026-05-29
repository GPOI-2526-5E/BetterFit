using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Gyms;
using Betterfit.Data;
using Betterfit.Infrastructure.Mapping;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Betterfit.Services.StaffAssignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/staff-assignments")]
public sealed class StaffAssignmentsController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountProfileService _accountProfileService;
    private readonly IStaffAssignmentService _staffAssignmentService;

    public StaffAssignmentsController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        IAccountProfileService accountProfileService,
        IStaffAssignmentService staffAssignmentService)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
        _accountProfileService = accountProfileService;
        _staffAssignmentService = staffAssignmentService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.StaffAssignmentsRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymStaffAssignmentResponse>>>> GetGymStaffAssignments(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymStaffAssignmentResponse>>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<GymStaffAssignmentResponse>>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            userId,
            gymId,
            PermissionResources.StaffAssignments,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymStaffAssignmentResponse>>();
        }

        var assignments = await LoadAssignmentsForGymAsync(gymId, cancellationToken);
        var visibleAssignments = assignments
            .Where(assignment => AssignmentIsVisible(assignment, scope))
            .Select(ResponseMappers.MapStaffAssignmentResponse)
            .ToList();

        return Success<IReadOnlyCollection<GymStaffAssignmentResponse>>(visibleAssignments);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.StaffAssignmentsWrite)]
    public async Task<ActionResult<ApiResponse<GymStaffAssignmentResponse>>> UpsertStaffAssignment(
        Guid gymId,
        [FromBody] CreateStaffAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymStaffAssignmentResponse>();
        }

        var gym = await _dbContext.Gyms
            .Include(x => x.Locations)
            .SingleOrDefaultAsync(x => x.Id == gymId, cancellationToken);

        if (gym is null)
        {
            return NotFoundError<GymStaffAssignmentResponse>("Gym not found.");
        }

        var role = await _dbContext.GymRoles
            .SingleOrDefaultAsync(role => role.Id == request.RoleId && role.GymId == gymId, cancellationToken);

        if (role is null)
        {
            return BadRequestError<GymStaffAssignmentResponse>("invalid_role_id", "Role not found in the target gym.");
        }

        var normalizedEmail = LocationHelpers.NormalizeEmail(request.Email);
        var user = await ResolveUserAsync(request.UserId, normalizedEmail);
        if (user is null)
        {
            return BadRequestError<GymStaffAssignmentResponse>(
                "invalid_target_user",
                "Provide a valid userId or email of an existing Betterfit account.");
        }

        // Normalize scope: tenant-scoped assignments must not have a ScopeLocationId
        var scopeType = request.ScopeType;
        var scopeLocationId = scopeType == TenantRoleAssignmentScopeType.Tenant ? null : request.ScopeLocationId;

        if (scopeType == TenantRoleAssignmentScopeType.Location)
        {
            if (!scopeLocationId.HasValue)
            {
                return BadRequestError<GymStaffAssignmentResponse>(
                    "missing_scope_location",
                    "Location-scoped assignments require a ScopeLocationId.");
            }

            if (!gym.Locations.Any(location => location.Id == scopeLocationId.Value && location.IsActive))
            {
                return BadRequestError<GymStaffAssignmentResponse>(
                    "invalid_scope_location",
                    "Scope location does not belong to the target gym or is inactive.");
            }
        }

        var staffProfile = await _accountProfileService.EnsureStaffProfileAsync(
            user,
            request.Profile,
            cancellationToken,
            overwriteProvidedValues: false);
        var result = await _staffAssignmentService.AssignAsync(
            gymId,
            user,
            staffProfile,
            role,
            scopeType,
            scopeLocationId,
            request.Status ?? TenantRoleAssignmentStatus.Active,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.IsConflict)
            {
                return ConflictError<GymStaffAssignmentResponse>(
                    result.ErrorMessage ?? "Staff assignment conflict.",
                    code: result.ErrorCode ?? "conflict");
            }

            return BadRequestError<GymStaffAssignmentResponse>(
                result.ErrorCode ?? "invalid_staff_assignment",
                result.ErrorMessage ?? "The staff assignment request is invalid.");
        }

        var saved = await LoadAssignmentAsync(result.Assignment!.Id, cancellationToken);
        return Success(ResponseMappers.MapStaffAssignmentResponse(saved!));
    }

    [HttpPut("{assignmentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.StaffAssignmentsWrite)]
    public async Task<ActionResult<ApiResponse<GymStaffAssignmentResponse>>> UpdateStaffAssignment(
        Guid gymId,
        Guid assignmentId,
        [FromBody] UpdateStaffAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await _dbContext.TenantRoleAssignments
            .Where(item => item.GymId == gymId)
            .Include(item => item.User)
            .Include(item => item.StaffProfile)
            .SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);

        if (assignment is null)
        {
            return NotFoundError<GymStaffAssignmentResponse>("Staff assignment not found for this gym.");
        }

        var gym = await _dbContext.Gyms
            .Include(item => item.Locations)
            .SingleOrDefaultAsync(item => item.Id == gymId, cancellationToken);

        if (gym is null)
        {
            return NotFoundError<GymStaffAssignmentResponse>("Gym not found.");
        }

        var role = await _dbContext.GymRoles
            .SingleOrDefaultAsync(role => role.Id == request.RoleId && role.GymId == gymId, cancellationToken);

        if (role is null)
        {
            return BadRequestError<GymStaffAssignmentResponse>("invalid_role_id", "Role not found in the target gym.");
        }

        var scopeType = request.ScopeType;
        var scopeLocationId = scopeType == TenantRoleAssignmentScopeType.Tenant ? null : request.ScopeLocationId;

        if (scopeType == TenantRoleAssignmentScopeType.Location)
        {
            if (!scopeLocationId.HasValue)
            {
                return BadRequestError<GymStaffAssignmentResponse>(
                    "missing_scope_location",
                    "Location-scoped assignments require a ScopeLocationId.");
            }

            if (!gym.Locations.Any(location => location.Id == scopeLocationId.Value && location.IsActive))
            {
                return BadRequestError<GymStaffAssignmentResponse>(
                    "invalid_scope_location",
                    "Scope location does not belong to the target gym or is inactive.");
            }
        }

        var staffProfile = await _accountProfileService.EnsureStaffProfileAsync(
            assignment.User,
            request.Profile,
            cancellationToken,
            overwriteProvidedValues: true);

        var result = await _staffAssignmentService.UpdateAsync(
            assignment,
            staffProfile,
            role,
            scopeType,
            scopeLocationId,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.IsConflict)
            {
                return ConflictError<GymStaffAssignmentResponse>(
                    result.ErrorMessage ?? "Staff assignment conflict.",
                    code: result.ErrorCode ?? "conflict");
            }

            return BadRequestError<GymStaffAssignmentResponse>(
                result.ErrorCode ?? "invalid_staff_assignment",
                result.ErrorMessage ?? "The staff assignment update is invalid.");
        }

        var saved = await LoadAssignmentAsync(assignmentId, cancellationToken);
        return Success(ResponseMappers.MapStaffAssignmentResponse(saved!));
    }

    private async Task<List<TenantRoleAssignment>> LoadAssignmentsForGymAsync(Guid gymId, CancellationToken cancellationToken)
    {
        return await _dbContext.TenantRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.GymId == gymId)
            .Include(assignment => assignment.Gym)
            .Include(assignment => assignment.User)
            .Include(assignment => assignment.Role)
            .Include(assignment => assignment.StaffProfile)
            .Include(assignment => assignment.ScopeLocation)
            .OrderBy(assignment => assignment.User.Email)
            .ThenBy(assignment => assignment.Role.Name)
            .ToListAsync(cancellationToken);
    }

    private async Task<TenantRoleAssignment?> LoadAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken)
    {
        return await _dbContext.TenantRoleAssignments
            .AsNoTracking()
            .Include(assignment => assignment.Gym)
            .Include(assignment => assignment.User)
            .Include(assignment => assignment.Role)
            .Include(assignment => assignment.StaffProfile)
            .Include(assignment => assignment.ScopeLocation)
            .SingleOrDefaultAsync(assignment => assignment.Id == assignmentId, cancellationToken);
    }

    private async Task<ApplicationUser?> ResolveUserAsync(string? userId, string? email)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var byId = await _userManager.FindByIdAsync(userId.Trim());
            if (byId is not null)
            {
                return byId;
            }
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return await _userManager.FindByEmailAsync(email);
        }

        return null;
    }

    private static bool AssignmentIsVisible(TenantRoleAssignment assignment, GymPermissionScope scope)
    {
        return assignment.ScopeType == TenantRoleAssignmentScopeType.Tenant
            ? scope.HasTenantWideAccess
            : assignment.ScopeLocationId.HasValue && scope.CanAccessLocation(assignment.ScopeLocationId.Value);
    }
}
