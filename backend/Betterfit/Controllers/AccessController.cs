using Betterfit.Authorization;
using Betterfit.Contracts.Access;
using Betterfit.Contracts.Common;
using Betterfit.Data;
using Betterfit.Infrastructure.Mapping;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/access")]
public sealed class AccessController : ApiControllerBase
{
    private static readonly TimeSpan DuplicateCheckinWindow = TimeSpan.FromMinutes(2);

    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccessController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet("overview")]
    [Authorize(Policy = AuthorizationPolicies.CheckinsApprove)]
    public async Task<ActionResult<ApiResponse<GymAccessOverviewResponse>>> GetOverview(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymAccessOverviewResponse>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<GymAccessOverviewResponse>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Checkins,
            PermissionActions.Approve,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymAccessOverviewResponse>();
        }

        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var lastThirtyMinutes = now.AddMinutes(-30);

        var events = await _dbContext.GymAccessEvents
            .AsNoTracking()
            .Where(accessEvent => accessEvent.GymId == gymId && accessEvent.OccurredAtUtc >= startOfToday)
            .Include(accessEvent => accessEvent.Location)
            .Include(accessEvent => accessEvent.Membership)
                .ThenInclude(membership => membership.User)
            .Include(accessEvent => accessEvent.Membership)
                .ThenInclude(membership => membership.MemberProfile)
            .OrderByDescending(accessEvent => accessEvent.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        var visibleEvents = events
            .Where(accessEvent => scope.CanAccessLocation(accessEvent.LocationId))
            .ToList();

        var deniedAttempts = visibleEvents
            .Where(accessEvent => accessEvent.Result == GymAccessEventResult.Denied && !string.IsNullOrWhiteSpace(accessEvent.Reason))
            .GroupBy(accessEvent => new
            {
                accessEvent.GymMembershipId,
                Reason = accessEvent.Reason!
            })
            .OrderByDescending(group => group.Max(accessEvent => accessEvent.OccurredAtUtc))
            .Take(6)
            .Select(group => new GymAccessDeniedAttemptResponse(
                MembershipDisplayName(group.First().Membership),
                group.Key.Reason,
                group.Count(),
                group.Max(accessEvent => accessEvent.OccurredAtUtc)))
            .ToList();

        var overview = new GymAccessOverviewResponse(
            visibleEvents
                .Where(accessEvent => accessEvent.Result == GymAccessEventResult.Granted)
                .Select(accessEvent => accessEvent.GymMembershipId)
                .Distinct()
                .Count(),
            visibleEvents.Count(accessEvent =>
                accessEvent.Result == GymAccessEventResult.Granted
                && accessEvent.OccurredAtUtc >= lastThirtyMinutes),
            visibleEvents.Count(accessEvent => accessEvent.Result == GymAccessEventResult.Denied),
            visibleEvents.Count(accessEvent =>
                accessEvent.Result == GymAccessEventResult.Granted
                && accessEvent.Origin == GymAccessOrigin.Desk),
            visibleEvents
                .Take(12)
                .Select(ResponseMappers.MapAccessEventResponse)
                .ToList(),
            deniedAttempts,
            now);

        return Success(overview);
    }

    [HttpGet("events")]
    [Authorize(Policy = AuthorizationPolicies.CheckinsApprove)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymAccessEventResponse>>>> GetEvents(
        Guid gymId,
        [FromQuery] GymAccessEventResult? result,
        [FromQuery] Guid? membershipId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymAccessEventResponse>>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<GymAccessEventResponse>>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Checkins,
            PermissionActions.Approve,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymAccessEventResponse>>();
        }

        var query = _dbContext.GymAccessEvents
            .AsNoTracking()
            .Where(accessEvent => accessEvent.GymId == gymId)
            .Include(accessEvent => accessEvent.Location)
            .Include(accessEvent => accessEvent.Membership)
                .ThenInclude(membership => membership.User)
            .Include(accessEvent => accessEvent.Membership)
                .ThenInclude(membership => membership.MemberProfile)
            .OrderByDescending(accessEvent => accessEvent.OccurredAtUtc)
            .AsQueryable();

        if (result.HasValue)
        {
            query = query.Where(accessEvent => accessEvent.Result == result.Value);
        }

        if (membershipId.HasValue)
        {
            query = query.Where(accessEvent => accessEvent.GymMembershipId == membershipId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(accessEvent => accessEvent.OccurredAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(accessEvent => accessEvent.OccurredAtUtc <= toUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(accessEvent =>
                accessEvent.GateName.ToLower().Contains(normalizedSearch)
                || (accessEvent.Reason != null && accessEvent.Reason.ToLower().Contains(normalizedSearch))
                || (accessEvent.Membership.User != null && accessEvent.Membership.User.Email != null && accessEvent.Membership.User.Email.ToLower().Contains(normalizedSearch))
                || (accessEvent.Membership.InvitationEmail != null && accessEvent.Membership.InvitationEmail.ToLower().Contains(normalizedSearch))
                || (accessEvent.Membership.MemberProfile != null
                    && (
                        (accessEvent.Membership.MemberProfile.FirstName != null && accessEvent.Membership.MemberProfile.FirstName.ToLower().Contains(normalizedSearch))
                        || (accessEvent.Membership.MemberProfile.LastName != null && accessEvent.Membership.MemberProfile.LastName.ToLower().Contains(normalizedSearch)))));
        }

        var events = await query.Take(200).ToListAsync(cancellationToken);
        var visibleEvents = events
            .Where(accessEvent => scope.CanAccessLocation(accessEvent.LocationId))
            .Select(ResponseMappers.MapAccessEventResponse)
            .ToList();

        return Success<IReadOnlyCollection<GymAccessEventResponse>>(visibleEvents);
    }

    [HttpPost("checkins")]
    [Authorize(Policy = AuthorizationPolicies.CheckinsApprove)]
    public async Task<ActionResult<ApiResponse<GymAccessEventResponse>>> RecordCheckin(
        Guid gymId,
        [FromBody] RecordGymCheckinRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymAccessEventResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Checkins,
            PermissionActions.Approve,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymAccessEventResponse>();
        }

        var membership = await _dbContext.GymMemberships
            .Include(entity => entity.User)
            .Include(entity => entity.MemberProfile)
            .Include(entity => entity.Locations)
                .ThenInclude(location => location.Location)
            .SingleOrDefaultAsync(entity => entity.Id == request.MembershipId && entity.GymId == gymId, cancellationToken);

        if (membership is null)
        {
            return NotFoundError<GymAccessEventResponse>("Membership not found.");
        }

        var locationId = ResolveLocationId(request.LocationId, membership);
        if (locationId == Guid.Empty)
        {
            return ConflictError<GymAccessEventResponse>("A valid location is required to record a check-in.");
        }

        if (!scope.CanAccessLocation(locationId))
        {
            return ForbiddenError<GymAccessEventResponse>("This check-in belongs to a location outside your staff scope.");
        }

        var location = membership.Locations
            .Select(item => item.Location)
            .SingleOrDefault(item => item.Id == locationId);

        if (location is null)
        {
            var deniedEvent = await RecordDeniedEventAsync(
                gymId,
                membership,
                locationId,
                request.GateName,
                actorUserId,
                "Membership is not enabled for the selected location.",
                cancellationToken);

            return Success(ResponseMappers.MapAccessEventResponse(deniedEvent));
        }

        if (!location.IsActive)
        {
            var deniedEvent = await RecordDeniedEventAsync(
                gymId,
                membership,
                locationId,
                request.GateName,
                actorUserId,
                "The selected location is not active.",
                cancellationToken);

            return Success(ResponseMappers.MapAccessEventResponse(deniedEvent));
        }

        var denialReason = ResolveCheckinDenialReason(membership);
        if (denialReason is not null)
        {
            var deniedEvent = await RecordDeniedEventAsync(
                gymId,
                membership,
                locationId,
                request.GateName,
                actorUserId,
                denialReason,
                cancellationToken);

            return Success(ResponseMappers.MapAccessEventResponse(deniedEvent));
        }

        var duplicateCutoff = DateTime.UtcNow.Subtract(DuplicateCheckinWindow);
        var hasRecentGrant = await _dbContext.GymAccessEvents.AnyAsync(
            accessEvent =>
                accessEvent.GymMembershipId == membership.Id
                && accessEvent.LocationId == locationId
                && accessEvent.Result == GymAccessEventResult.Granted
                && accessEvent.OccurredAtUtc >= duplicateCutoff,
            cancellationToken);

        if (hasRecentGrant)
        {
            var deniedEvent = await RecordDeniedEventAsync(
                gymId,
                membership,
                locationId,
                request.GateName,
                actorUserId,
                "Duplicate check-in detected too close to the previous access.",
                cancellationToken);

            return Success(ResponseMappers.MapAccessEventResponse(deniedEvent));
        }

        var grantedEvent = new GymAccessEvent
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            GymMembershipId = membership.Id,
            LocationId = locationId,
            GateName = NormalizeGateName(request.GateName),
            Result = GymAccessEventResult.Granted,
            Origin = GymAccessOrigin.Desk,
            OccurredAtUtc = DateTime.UtcNow,
            PerformedByUserId = actorUserId
        };

        _dbContext.GymAccessEvents.Add(grantedEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedEvent = await LoadAccessEventAsync(grantedEvent.Id, cancellationToken);
        return Success(ResponseMappers.MapAccessEventResponse(savedEvent!));
    }

    private async Task<GymAccessEvent> RecordDeniedEventAsync(
        Guid gymId,
        GymMembership membership,
        Guid locationId,
        string? gateName,
        string actorUserId,
        string reason,
        CancellationToken cancellationToken)
    {
        var deniedEvent = new GymAccessEvent
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            GymMembershipId = membership.Id,
            LocationId = locationId,
            GateName = NormalizeGateName(gateName),
            Result = GymAccessEventResult.Denied,
            Origin = GymAccessOrigin.Desk,
            Reason = reason,
            OccurredAtUtc = DateTime.UtcNow,
            PerformedByUserId = actorUserId
        };

        _dbContext.GymAccessEvents.Add(deniedEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await LoadAccessEventAsync(deniedEvent.Id, cancellationToken))!;
    }

    private async Task<GymAccessEvent?> LoadAccessEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _dbContext.GymAccessEvents
            .AsNoTracking()
            .Include(accessEvent => accessEvent.Location)
            .Include(accessEvent => accessEvent.Membership)
                .ThenInclude(membership => membership.User)
            .Include(accessEvent => accessEvent.Membership)
                .ThenInclude(membership => membership.MemberProfile)
            .SingleOrDefaultAsync(accessEvent => accessEvent.Id == eventId, cancellationToken);
    }

    private static Guid ResolveLocationId(Guid? requestedLocationId, GymMembership membership)
    {
        if (requestedLocationId is { } explicitLocationId && explicitLocationId != Guid.Empty)
        {
            return explicitLocationId;
        }

        if (membership.PrimaryLocationId is { } primaryLocationId && primaryLocationId != Guid.Empty)
        {
            return primaryLocationId;
        }

        return membership.Locations
            .OrderBy(location => location.Location.Name)
            .Select(location => location.LocationId)
            .FirstOrDefault();
    }

    private static string? ResolveCheckinDenialReason(GymMembership membership)
    {
        if (membership.Status == GymMembershipStatus.PendingClaim)
        {
            return "Membership claim is still pending.";
        }

        if (membership.Status == GymMembershipStatus.Suspended)
        {
            return "Membership is suspended.";
        }

        if (membership.Status == GymMembershipStatus.Archived)
        {
            return "Membership is archived.";
        }

        if (membership.EndedAtUtc is { } endedAtUtc && endedAtUtc <= DateTime.UtcNow)
        {
            return "Membership has expired.";
        }

        return null;
    }

    private static string NormalizeGateName(string? gateName)
    {
        return string.IsNullOrWhiteSpace(gateName) ? "Desk check-in" : gateName.Trim();
    }

    private static string MembershipDisplayName(GymMembership membership)
    {
        var firstName = membership.MemberProfile?.FirstName?.Trim() ?? membership.PendingFirstName?.Trim();
        var lastName = membership.MemberProfile?.LastName?.Trim() ?? membership.PendingLastName?.Trim();
        var fullName = string.Join(" ", new[] { firstName, lastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return membership.User?.Email?.Trim()
            ?? membership.InvitationEmail?.Trim()
            ?? "Cliente senza nome";
    }
}
