using Betterfit.Authorization;
using Betterfit.Contracts.Activities;
using Betterfit.Contracts.Common;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/activities")]
public sealed class ActivitiesController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivitiesController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet("overview")]
    [Authorize(Policy = AuthorizationPolicies.ClassesRead)]
    public async Task<ActionResult<ApiResponse<GymActivitiesOverviewResponse>>> GetOverview(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymActivitiesOverviewResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Classes,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymActivitiesOverviewResponse>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        if (allowedLocationIds.Count == 0)
        {
            return Success(new GymActivitiesOverviewResponse(0, 0, 0, 0, [], DateTime.UtcNow));
        }

        var now = DateTime.UtcNow;
        var nextWeek = now.AddDays(7);
        var startOfToday = now.Date;
        var last30Days = startOfToday.AddDays(-30);

        var sessions = await LoadSessionsQuery(gymId)
            .Where(session => allowedLocationIds.Contains(session.LocationId) && session.StartsAtUtc >= startOfToday)
            .OrderBy(session => session.StartsAtUtc)
            .Take(24)
            .ToListAsync(cancellationToken);

        var overview = new GymActivitiesOverviewResponse(
            sessions.Count(session => session.StartsAtUtc <= nextWeek && session.Status == GymActivitySessionStatus.Scheduled),
            sessions
                .Where(session => session.StartsAtUtc <= nextWeek && session.Status == GymActivitySessionStatus.Scheduled)
                .Sum(session => session.Bookings.Count(booking => booking.Status is GymActivityBookingStatus.Booked or GymActivityBookingStatus.CheckedIn)),
            sessions
                .Where(session => session.StartsAtUtc >= startOfToday)
                .Sum(session => session.Bookings.Count(booking => booking.Status == GymActivityBookingStatus.CheckedIn)),
            await _dbContext.GymActivityBookings
                .AsNoTracking()
                .Where(booking =>
                    booking.GymId == gymId
                    && booking.Status == GymActivityBookingStatus.NoShow
                    && booking.BookedAtUtc >= last30Days
                    && allowedLocationIds.Contains(booking.Session.LocationId))
                .CountAsync(cancellationToken),
            sessions.Select(MapSessionResponse).ToList(),
            now);

        return Success(overview);
    }

    [HttpGet("templates")]
    [Authorize(Policy = AuthorizationPolicies.ClassesRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymActivityTemplateResponse>>>> GetTemplates(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var scope = await GetClassesScopeAsync(gymId, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymActivityTemplateResponse>>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var templates = await _dbContext.GymActivityTemplates
            .AsNoTracking()
            .Where(template => template.GymId == gymId && allowedLocationIds.Contains(template.LocationId))
            .Include(template => template.Location)
            .Include(template => template.InstructorAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(template => template.InstructorAssignment)
                .ThenInclude(assignment => assignment!.User)
            .OrderBy(template => template.Name)
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymActivityTemplateResponse>>(templates.Select(MapTemplateResponse).ToList());
    }

    [HttpPost("templates")]
    [Authorize(Policy = AuthorizationPolicies.ClassesWrite)]
    public async Task<ActionResult<ApiResponse<GymActivityTemplateResponse>>> CreateTemplate(
        Guid gymId,
        [FromBody] CreateGymActivityTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymActivityTemplateResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Classes,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess || !scope.CanAccessLocation(request.LocationId))
        {
            return ForbiddenError<GymActivityTemplateResponse>();
        }

        var location = await _dbContext.GymLocations
            .SingleOrDefaultAsync(location => location.Id == request.LocationId && location.GymId == gymId && location.IsActive, cancellationToken);

        if (location is null)
        {
            return BadRequestError<GymActivityTemplateResponse>("invalid_location", "The selected location is not valid for this gym.");
        }

        var instructorAssignment = await ResolveInstructorAssignmentAsync(gymId, request.InstructorAssignmentId, cancellationToken);
        if (request.InstructorAssignmentId.HasValue && instructorAssignment is null)
        {
            return BadRequestError<GymActivityTemplateResponse>("invalid_instructor", "The selected instructor assignment is not valid.");
        }

        var now = DateTime.UtcNow;
        var template = new GymActivityTemplate
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            LocationId = location.Id,
            InstructorAssignmentId = instructorAssignment?.Id,
            Name = request.Name.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Corso" : request.Category.Trim(),
            Description = NormalizeOptional(request.Description),
            ColorHex = NormalizeOptional(request.ColorHex),
            Capacity = request.Capacity,
            DurationMinutes = request.DurationMinutes,
            RequiresBooking = request.RequiresBooking,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymActivityTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await _dbContext.GymActivityTemplates
            .AsNoTracking()
            .Include(item => item.Location)
            .Include(item => item.InstructorAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(item => item.InstructorAssignment)
                .ThenInclude(assignment => assignment!.User)
            .SingleAsync(item => item.Id == template.Id, cancellationToken);

        return Success(MapTemplateResponse(saved));
    }

    [HttpPatch("templates/{templateId:guid}/activation")]
    [Authorize(Policy = AuthorizationPolicies.ClassesWrite)]
    public async Task<ActionResult<ApiResponse<GymActivityTemplateResponse>>> UpdateTemplateActivation(
        Guid gymId,
        Guid templateId,
        [FromBody] UpdateGymActivityTemplateActivationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymActivityTemplateResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Classes,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymActivityTemplateResponse>();
        }

        var template = await _dbContext.GymActivityTemplates
            .Include(item => item.Location)
            .Include(item => item.InstructorAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(item => item.InstructorAssignment)
                .ThenInclude(assignment => assignment!.User)
            .SingleOrDefaultAsync(item => item.Id == templateId && item.GymId == gymId, cancellationToken);

        if (template is null)
        {
            return NotFoundError<GymActivityTemplateResponse>("Template not found.");
        }

        if (!scope.CanAccessLocation(template.LocationId))
        {
            return ForbiddenError<GymActivityTemplateResponse>();
        }

        if (template.IsActive == request.IsActive)
        {
            return ConflictError<GymActivityTemplateResponse>("The template is already in the selected state.");
        }

        template.IsActive = request.IsActive;
        template.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success(MapTemplateResponse(template));
    }

    [HttpGet("sessions")]
    [Authorize(Policy = AuthorizationPolicies.ClassesRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymActivitySessionResponse>>>> GetSessions(
        Guid gymId,
        [FromQuery] Guid? locationId,
        [FromQuery] Guid? membershipId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var scope = await GetClassesScopeAsync(gymId, cancellationToken);
        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymActivitySessionResponse>>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, scope, locationId, cancellationToken);
        var query = LoadSessionsQuery(gymId)
            .Where(session => allowedLocationIds.Contains(session.LocationId));

        if (fromUtc.HasValue)
        {
            query = query.Where(session => session.StartsAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(session => session.StartsAtUtc <= toUtc.Value);
        }

        if (membershipId.HasValue)
        {
            query = query.Where(session => session.Bookings.Any(booking => booking.GymMembershipId == membershipId.Value));
        }

        var sessions = await query
            .OrderBy(session => session.StartsAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymActivitySessionResponse>>(sessions.Select(MapSessionResponse).ToList());
    }

    [HttpPost("sessions")]
    [Authorize(Policy = AuthorizationPolicies.ClassesWrite)]
    public async Task<ActionResult<ApiResponse<GymActivitySessionResponse>>> CreateSession(
        Guid gymId,
        [FromBody] CreateGymActivitySessionRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymActivitySessionResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Classes,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymActivitySessionResponse>();
        }

        var template = await _dbContext.GymActivityTemplates
            .Include(item => item.Location)
            .Include(item => item.InstructorAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(item => item.InstructorAssignment)
                .ThenInclude(assignment => assignment!.User)
            .SingleOrDefaultAsync(item => item.Id == request.TemplateId && item.GymId == gymId && item.IsActive, cancellationToken);

        if (template is null)
        {
            return BadRequestError<GymActivitySessionResponse>("invalid_template", "The selected activity template is not valid.");
        }

        if (!scope.CanAccessLocation(template.LocationId))
        {
            return ForbiddenError<GymActivitySessionResponse>("This session belongs to a location outside your current scope.");
        }

        var startsAtUtc = request.StartsAtUtc;
        var endsAtUtc = request.EndsAtUtc ?? startsAtUtc.AddMinutes(template.DurationMinutes);
        if (endsAtUtc <= startsAtUtc)
        {
            return BadRequestError<GymActivitySessionResponse>("invalid_schedule", "Session end time must be after start time.");
        }

        var instructorName = ResolveInstructorName(template.InstructorAssignment);
        var now = DateTime.UtcNow;
        var session = new GymActivitySession
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            GymActivityTemplateId = template.Id,
            LocationId = template.LocationId,
            InstructorAssignmentId = template.InstructorAssignmentId,
            Title = template.Name,
            InstructorName = instructorName,
            Capacity = template.Capacity,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            Status = GymActivitySessionStatus.Scheduled,
            Notes = NormalizeOptional(request.Notes),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymActivitySessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadSessionsQuery(gymId)
            .SingleAsync(item => item.Id == session.Id, cancellationToken);

        return Success(MapSessionResponse(saved));
    }

    [HttpPatch("sessions/{sessionId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.ClassesWrite)]
    public async Task<ActionResult<ApiResponse<GymActivitySessionResponse>>> UpdateSessionStatus(
        Guid gymId,
        Guid sessionId,
        [FromBody] UpdateGymActivitySessionStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymActivitySessionResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Classes,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymActivitySessionResponse>();
        }

        var session = await _dbContext.GymActivitySessions
            .Include(item => item.Bookings)
            .SingleOrDefaultAsync(item => item.Id == sessionId && item.GymId == gymId, cancellationToken);

        if (session is null)
        {
            return NotFoundError<GymActivitySessionResponse>("Session not found.");
        }

        if (!scope.CanAccessLocation(session.LocationId))
        {
            return ForbiddenError<GymActivitySessionResponse>();
        }

        if (session.Status == request.Status)
        {
            return ConflictError<GymActivitySessionResponse>("The session is already in the selected status.");
        }

        if (request.Status == GymActivitySessionStatus.Cancelled
            && session.Bookings.Any(item => item.Status == GymActivityBookingStatus.CheckedIn))
        {
            return ConflictError<GymActivitySessionResponse>("Sessions with checked-in attendees cannot be cancelled.");
        }

        var now = DateTime.UtcNow;
        session.Status = request.Status;
        session.UpdatedAtUtc = now;

        if (request.Status == GymActivitySessionStatus.Cancelled)
        {
            foreach (var booking in session.Bookings.Where(item =>
                         item.Status is GymActivityBookingStatus.Booked or GymActivityBookingStatus.CheckedIn))
            {
                booking.Status = GymActivityBookingStatus.Cancelled;
                booking.CheckedInAtUtc = null;
                booking.CancelledAtUtc = now;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadSessionsQuery(gymId)
            .SingleAsync(item => item.Id == session.Id, cancellationToken);

        return Success(MapSessionResponse(saved));
    }

    [HttpPost("sessions/{sessionId:guid}/bookings")]
    [Authorize(Policy = AuthorizationPolicies.ClassesWrite)]
    public async Task<ActionResult<ApiResponse<GymActivitySessionResponse>>> CreateBooking(
        Guid gymId,
        Guid sessionId,
        [FromBody] CreateGymActivityBookingRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymActivitySessionResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Classes,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymActivitySessionResponse>();
        }

        var session = await _dbContext.GymActivitySessions
            .Include(item => item.Template)
            .Include(item => item.Location)
            .Include(item => item.Bookings)
            .SingleOrDefaultAsync(item => item.Id == sessionId && item.GymId == gymId, cancellationToken);

        if (session is null)
        {
            return NotFoundError<GymActivitySessionResponse>("Session not found.");
        }

        if (!scope.CanAccessLocation(session.LocationId))
        {
            return ForbiddenError<GymActivitySessionResponse>();
        }

        if (session.Status != GymActivitySessionStatus.Scheduled)
        {
            return ConflictError<GymActivitySessionResponse>("Only scheduled sessions can accept bookings.");
        }

        var membership = await _dbContext.GymMemberships
            .Include(item => item.User)
            .Include(item => item.MemberProfile)
            .Include(item => item.Locations)
            .SingleOrDefaultAsync(item => item.Id == request.MembershipId && item.GymId == gymId, cancellationToken);

        if (membership is null)
        {
            return NotFoundError<GymActivitySessionResponse>("Membership not found.");
        }

        if (!membership.Locations.Any(item => item.LocationId == session.LocationId))
        {
            return ConflictError<GymActivitySessionResponse>("The selected member is not enabled for this session location.");
        }

        if (session.Bookings.Any(item => item.GymMembershipId == membership.Id))
        {
            return ConflictError<GymActivitySessionResponse>("This member is already booked for the selected session.");
        }

        var activeBookingsCount = session.Bookings.Count(item => item.Status is GymActivityBookingStatus.Booked or GymActivityBookingStatus.CheckedIn);
        if (activeBookingsCount >= session.Capacity)
        {
            return ConflictError<GymActivitySessionResponse>("No remaining spots are available for this session.");
        }

        var booking = new GymActivityBooking
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            GymActivitySessionId = session.Id,
            GymMembershipId = membership.Id,
            Status = GymActivityBookingStatus.Booked,
            BookedAtUtc = DateTime.UtcNow,
            Notes = NormalizeOptional(request.Notes)
        };

        _dbContext.GymActivityBookings.Add(booking);
        session.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadSessionsQuery(gymId)
            .SingleAsync(item => item.Id == session.Id, cancellationToken);

        return Success(MapSessionResponse(saved));
    }

    [HttpPost("bookings/{bookingId:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.ClassesWrite)]
    public async Task<ActionResult<ApiResponse<GymActivitySessionResponse>>> UpdateBookingStatus(
        Guid gymId,
        Guid bookingId,
        [FromBody] UpdateGymActivityBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymActivitySessionResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Classes,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymActivitySessionResponse>();
        }

        var booking = await _dbContext.GymActivityBookings
            .Include(item => item.Session)
            .SingleOrDefaultAsync(item => item.Id == bookingId && item.GymId == gymId, cancellationToken);

        if (booking is null)
        {
            return NotFoundError<GymActivitySessionResponse>("Booking not found.");
        }

        if (!scope.CanAccessLocation(booking.Session.LocationId))
        {
            return ForbiddenError<GymActivitySessionResponse>();
        }

        if (request.Status is GymActivityBookingStatus.Booked or GymActivityBookingStatus.CheckedIn
            && booking.Session.Status != GymActivitySessionStatus.Scheduled)
        {
            return ConflictError<GymActivitySessionResponse>("Only scheduled sessions can keep active bookings or check-ins.");
        }

        booking.Status = request.Status;
        booking.Notes = NormalizeOptional(request.Notes) ?? booking.Notes;
        booking.CheckedInAtUtc = request.Status == GymActivityBookingStatus.CheckedIn ? DateTime.UtcNow : null;
        booking.CancelledAtUtc = request.Status == GymActivityBookingStatus.Cancelled ? DateTime.UtcNow : null;
        booking.Session.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await LoadSessionsQuery(gymId)
            .SingleAsync(item => item.Id == booking.Session.Id, cancellationToken);

        return Success(MapSessionResponse(saved));
    }

    private async Task<GymPermissionScope> GetClassesScopeAsync(Guid gymId, CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return GymPermissionScope.None;
        }

        return await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Classes,
            PermissionActions.Read,
            cancellationToken);
    }

    private async Task<HashSet<Guid>> ResolveAllowedLocationIdsAsync(
        Guid gymId,
        GymPermissionScope scope,
        Guid? requestedLocationId,
        CancellationToken cancellationToken)
    {
        var locations = await _dbContext.GymLocations
            .AsNoTracking()
            .Where(location => location.GymId == gymId && location.IsActive)
            .Select(location => location.Id)
            .ToListAsync(cancellationToken);

        var allowedLocationIds = locations
            .Where(scope.CanAccessLocation)
            .ToHashSet();

        if (requestedLocationId.HasValue)
        {
            return allowedLocationIds.Contains(requestedLocationId.Value)
                ? [requestedLocationId.Value]
                : [];
        }

        return allowedLocationIds;
    }

    private async Task<TenantRoleAssignment?> ResolveInstructorAssignmentAsync(
        Guid gymId,
        Guid? instructorAssignmentId,
        CancellationToken cancellationToken)
    {
        if (!instructorAssignmentId.HasValue)
        {
            return null;
        }

        return await _dbContext.TenantRoleAssignments
            .Include(item => item.StaffProfile)
            .Include(item => item.User)
            .SingleOrDefaultAsync(
                item => item.Id == instructorAssignmentId.Value
                    && item.GymId == gymId
                    && item.Status == TenantRoleAssignmentStatus.Active
                    && item.RevokedAtUtc == null,
                cancellationToken);
    }

    private IQueryable<GymActivitySession> LoadSessionsQuery(Guid gymId)
    {
        return _dbContext.GymActivitySessions
            .AsNoTracking()
            .Where(session => session.GymId == gymId)
            .Include(session => session.Template)
            .Include(session => session.Location)
            .Include(session => session.Bookings)
                .ThenInclude(booking => booking.Membership)
                    .ThenInclude(membership => membership.User)
            .Include(session => session.Bookings)
                .ThenInclude(booking => booking.Membership)
                    .ThenInclude(membership => membership.MemberProfile);
    }

    private static GymActivityTemplateResponse MapTemplateResponse(GymActivityTemplate template)
    {
        return new GymActivityTemplateResponse(
            template.Id,
            template.GymId,
            template.LocationId,
            template.Location?.Name ?? "Sede non disponibile",
            template.InstructorAssignmentId,
            ResolveInstructorName(template.InstructorAssignment),
            template.Name,
            template.Category,
            template.Description,
            template.ColorHex,
            template.Capacity,
            template.DurationMinutes,
            template.RequiresBooking,
            template.IsActive,
            template.CreatedAtUtc,
            template.UpdatedAtUtc);
    }

    private static GymActivitySessionResponse MapSessionResponse(GymActivitySession session)
    {
        var activeBookingsCount = session.Bookings.Count(booking =>
            booking.Status is GymActivityBookingStatus.Booked or GymActivityBookingStatus.CheckedIn);
        var checkedInCount = session.Bookings.Count(booking => booking.Status == GymActivityBookingStatus.CheckedIn);

        return new GymActivitySessionResponse(
            session.Id,
            session.GymActivityTemplateId,
            session.GymId,
            session.LocationId,
            session.Location?.Name ?? "Sede non disponibile",
            session.InstructorAssignmentId,
            session.InstructorName,
            session.Title,
            session.Template?.Category ?? "Corso",
            session.Template?.ColorHex,
            session.Capacity,
            activeBookingsCount,
            checkedInCount,
            Math.Max(0, session.Capacity - activeBookingsCount),
            session.StartsAtUtc,
            session.EndsAtUtc,
            session.Status,
            session.Notes,
            session.Bookings
                .OrderBy(booking => booking.BookedAtUtc)
                .Select(MapBookingResponse)
                .ToList());
    }

    private static GymActivityBookingResponse MapBookingResponse(GymActivityBooking booking)
    {
        return new GymActivityBookingResponse(
            booking.Id,
            booking.GymActivitySessionId,
            booking.GymMembershipId,
            MembershipDisplayName(booking.Membership),
            booking.Membership.User?.Email?.Trim()
                ?? booking.Membership.InvitationEmail?.Trim()
                ?? "Email non disponibile",
            booking.Status,
            booking.BookedAtUtc,
            booking.CheckedInAtUtc,
            booking.CancelledAtUtc,
            booking.Notes);
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

    private static string ResolveInstructorName(TenantRoleAssignment? assignment)
    {
        var displayName = assignment?.StaffProfile?.DisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        return assignment?.User?.FullName?.Trim()
            ?? assignment?.User?.Email?.Trim()
            ?? "Istruttore da assegnare";
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
