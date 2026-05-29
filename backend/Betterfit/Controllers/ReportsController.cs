using System.Globalization;
using System.Text;
using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Reports;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/reports")]
public sealed class ReportsController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet("kpi")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    public async Task<ActionResult<ApiResponse<GymKpiDashboardResponse>>> GetKpiDashboard(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymKpiDashboardResponse>();
        }

        var gymName = await _dbContext.Gyms
            .AsNoTracking()
            .Where(gym => gym.Id == gymId)
            .Select(gym => gym.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(gymName))
        {
            return NotFoundError<GymKpiDashboardResponse>("Gym not found.");
        }

        var reportsScope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Reports,
            PermissionActions.Read,
            cancellationToken);

        if (!reportsScope.HasAnyAccess)
        {
            return ForbiddenError<GymKpiDashboardResponse>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, reportsScope, locationId, cancellationToken);
        if (allowedLocationIds.Count == 0)
        {
            return Success(new GymKpiDashboardResponse(
                0, 0m, 0m, 0, 0m, 0, 0, 0, [], [], [], BuildLeadPipeline([]), [], DateTime.UtcNow));
        }

        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var startOfMonth = new DateTime(startOfToday.Year, startOfToday.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var last90Days = startOfToday.AddDays(-90);
        var next7Days = now.AddDays(7);
        var last30Days = startOfToday.AddDays(-30);

        var locations = await _dbContext.GymLocations
            .AsNoTracking()
            .Where(location => location.GymId == gymId && allowedLocationIds.Contains(location.Id))
            .OrderBy(location => location.Name)
            .ToListAsync(cancellationToken);

        var memberships = await _dbContext.GymMemberships
            .AsNoTracking()
            .Where(membership => membership.GymId == gymId && membership.Status == GymMembershipStatus.Active)
            .Include(membership => membership.Locations)
            .ToListAsync(cancellationToken);

        var visibleMemberships = memberships
            .Where(membership =>
                membership.PrimaryLocationId is { } primaryLocationId && allowedLocationIds.Contains(primaryLocationId)
                || membership.Locations.Any(location => allowedLocationIds.Contains(location.LocationId)))
            .ToList();

        var sales = await _dbContext.GymSales
            .AsNoTracking()
            .Where(sale => sale.GymId == gymId && allowedLocationIds.Contains(sale.LocationId))
            .Include(sale => sale.Location)
            .ToListAsync(cancellationToken);

        var activeSales = sales
            .Where(sale => sale.Status is not (GymSaleStatus.Cancelled or GymSaleStatus.Refunded))
            .ToList();

        var accessEvents = await _dbContext.GymAccessEvents
            .AsNoTracking()
            .Where(accessEvent => accessEvent.GymId == gymId && allowedLocationIds.Contains(accessEvent.LocationId) && accessEvent.OccurredAtUtc >= startOfToday)
            .Include(accessEvent => accessEvent.Location)
            .ToListAsync(cancellationToken);

        var sessions = await _dbContext.GymActivitySessions
            .AsNoTracking()
            .Where(session => session.GymId == gymId && allowedLocationIds.Contains(session.LocationId) && session.StartsAtUtc >= now && session.StartsAtUtc <= next7Days)
            .Include(session => session.Location)
            .Include(session => session.Bookings)
            .OrderBy(session => session.StartsAtUtc)
            .Take(12)
            .ToListAsync(cancellationToken);

        var workoutAssignments = await _dbContext.GymWorkoutAssignments
            .AsNoTracking()
            .Where(assignment => assignment.GymId == gymId && allowedLocationIds.Contains(assignment.LocationId))
            .Include(assignment => assignment.Location)
            .ToListAsync(cancellationToken);

        var workoutAssessments = await _dbContext.GymWorkoutAssessments
            .AsNoTracking()
            .Where(assessment => assessment.GymId == gymId && allowedLocationIds.Contains(assessment.LocationId) && assessment.RecordedAtUtc >= last30Days)
            .ToListAsync(cancellationToken);

        var leads = await _dbContext.GymLeads
            .AsNoTracking()
            .Where(lead => lead.GymId == gymId && allowedLocationIds.Contains(lead.LocationId))
            .ToListAsync(cancellationToken);

        var revenueByLocation = locations
            .Select(location =>
            {
                var locationSales = activeSales.Where(sale => sale.LocationId == location.Id).ToList();
                var monthSalesCount = locationSales.Count(sale => sale.SoldAtUtc >= startOfMonth);
                var revenueMonthAmount = locationSales
                    .Where(sale => sale.SoldAtUtc >= startOfMonth)
                    .Sum(sale => sale.PaidAmount);
                var pendingCollectionsAmount = locationSales.Sum(sale => sale.RemainingAmount);

                return IsTenantPlaceholderRevenueLocation(
                    location,
                    gymName,
                    monthSalesCount,
                    revenueMonthAmount,
                    pendingCollectionsAmount)
                    ? null
                    : new GymKpiLocationRevenueResponse(
                        location.Id,
                        location.Name,
                        monthSalesCount,
                        revenueMonthAmount,
                        pendingCollectionsAmount);
            })
            .OfType<GymKpiLocationRevenueResponse>()
            .ToList();

        var accessByLocation = locations
            .Select(location =>
            {
                var locationAccess = accessEvents.Where(accessEvent => accessEvent.LocationId == location.Id).ToList();
                return new GymKpiLocationAccessResponse(
                    location.Id,
                    location.Name,
                    locationAccess.Count(accessEvent => accessEvent.Result == GymAccessEventResult.Granted),
                    locationAccess.Count(accessEvent => accessEvent.Result == GymAccessEventResult.Denied));
            })
            .ToList();

        var upcomingActivities = sessions
            .Select(session =>
            {
                var bookedCount = session.Bookings.Count(booking => booking.Status is GymActivityBookingStatus.Booked or GymActivityBookingStatus.CheckedIn);
                var occupancyRate = session.Capacity <= 0
                    ? 0m
                    : decimal.Round((decimal)bookedCount / session.Capacity * 100m, 1, MidpointRounding.AwayFromZero);

                return new GymKpiUpcomingActivityResponse(
                    session.Id,
                    session.LocationId,
                    session.Location.Name,
                    session.Title,
                    session.InstructorName,
                    session.StartsAtUtc,
                    session.Capacity,
                    bookedCount,
                    occupancyRate,
                    session.Status);
            })
            .ToList();

        var trainingByLocation = locations
            .Select(location =>
            {
                var locationAssignments = workoutAssignments.Where(assignment => assignment.LocationId == location.Id).ToList();
                return new GymKpiTrainingSummaryResponse(
                    location.Id,
                    location.Name,
                    locationAssignments.Count(assignment => assignment.Status == GymWorkoutAssignmentStatus.Active),
                    locationAssignments.Count(assignment =>
                        assignment.Status == GymWorkoutAssignmentStatus.Active
                        && assignment.RevisionDueAtUtc.HasValue
                        && assignment.RevisionDueAtUtc.Value <= next7Days),
                    workoutAssessments.Count(assessment => assessment.LocationId == location.Id));
            })
            .ToList();

        var pipelineLeads = leads
            .Where(lead => lead.Stage is not (GymLeadStage.Won or GymLeadStage.Lost))
            .ToList();
        var wonLeadsLast90Days = leads
            .Count(lead => lead.Stage == GymLeadStage.Won && lead.UpdatedAtUtc >= last90Days);
        var consideredLeadsLast90Days = leads.Count(lead => lead.CreatedAtUtc >= last90Days);
        var conversionRate = consideredLeadsLast90Days == 0
            ? 0m
            : decimal.Round((decimal)wonLeadsLast90Days / consideredLeadsLast90Days * 100m, 1, MidpointRounding.AwayFromZero);

        var response = new GymKpiDashboardResponse(
            visibleMemberships.Count,
            activeSales.Where(sale => sale.SoldAtUtc >= startOfMonth).Sum(sale => sale.PaidAmount),
            activeSales.Sum(sale => sale.RemainingAmount),
            pipelineLeads.Count,
            conversionRate,
            accessEvents.Count(accessEvent => accessEvent.Result == GymAccessEventResult.Granted),
            upcomingActivities.Sum(session => session.BookedCount),
            workoutAssignments.Count(assignment => assignment.Status == GymWorkoutAssignmentStatus.Active),
            revenueByLocation,
            accessByLocation,
            upcomingActivities,
            BuildLeadPipeline(leads),
            trainingByLocation,
            now);

        return Success(response);
    }

    [HttpGet("churn")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    public async Task<ActionResult<ApiResponse<GymRetentionChurnResponse>>> GetRetentionChurn(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymRetentionChurnResponse>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<GymRetentionChurnResponse>("Gym not found.");
        }

        var reportsScope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Reports,
            PermissionActions.Read,
            cancellationToken);

        if (!reportsScope.HasAnyAccess)
        {
            return ForbiddenError<GymRetentionChurnResponse>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, reportsScope, locationId, cancellationToken);
        if (allowedLocationIds.Count == 0)
        {
            return Success(new GymRetentionChurnResponse(0, 0, 0, 0, 0, 0m, 0m, [], [], DateTime.UtcNow));
        }

        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var next30Days = startOfToday.AddDays(30);
        var last90Days = startOfToday.AddDays(-90);

        var retentionDataset = await LoadRetentionDatasetAsync(gymId, allowedLocationIds, last90Days, cancellationToken);
        var locations = retentionDataset.Locations;
        var analyzedMemberships = retentionDataset.Memberships;

        var activeMembers = analyzedMemberships
            .Where(item => item.Membership.Status == GymMembershipStatus.Active)
            .ToList();

        var expiringNext30Days = activeMembers
            .Where(item =>
                item.Membership.EndedAtUtc.HasValue
                && item.Membership.EndedAtUtc.Value >= startOfToday
                && item.Membership.EndedAtUtc.Value <= next30Days)
            .ToList();

        var atRiskMembers = expiringNext30Days
            .Where(item => !item.HasRenewalSale)
            .OrderBy(item => item.Membership.EndedAtUtc)
            .ThenBy(item => BuildMembershipDisplayName(item.Membership))
            .ToList();

        var expiredLast90Days = analyzedMemberships
            .Where(item =>
                item.Membership.EndedAtUtc.HasValue
                && item.Membership.EndedAtUtc.Value >= last90Days
                && item.Membership.EndedAtUtc.Value < startOfToday)
            .ToList();

        var renewedLast90Days = expiredLast90Days
            .Count(item => item.HasRenewalSale);
        var churnedLast90Days = expiredLast90Days
            .Count(item => !item.HasRenewalSale);
        var retentionRate = expiredLast90Days.Count == 0
            ? 0m
            : decimal.Round((decimal)renewedLast90Days / expiredLast90Days.Count * 100m, 1, MidpointRounding.AwayFromZero);
        var churnRate = expiredLast90Days.Count == 0
            ? 0m
            : decimal.Round((decimal)churnedLast90Days / expiredLast90Days.Count * 100m, 1, MidpointRounding.AwayFromZero);

        var locationSummaries = locations
            .Select(location =>
            {
                var locationMemberships = analyzedMemberships.Where(item => item.ReportLocation!.Id == location.Id).ToList();
                var locationActiveMembers = locationMemberships.Where(item => item.Membership.Status == GymMembershipStatus.Active).ToList();
                var locationExpiring = locationActiveMembers.Where(item =>
                    item.Membership.EndedAtUtc.HasValue
                    && item.Membership.EndedAtUtc.Value >= startOfToday
                    && item.Membership.EndedAtUtc.Value <= next30Days).ToList();
                var locationExpired = locationMemberships.Where(item =>
                    item.Membership.EndedAtUtc.HasValue
                    && item.Membership.EndedAtUtc.Value >= last90Days
                    && item.Membership.EndedAtUtc.Value < startOfToday).ToList();
                var locationRenewed = locationExpired.Count(item => item.HasRenewalSale);
                var locationChurned = locationExpired.Count - locationRenewed;
                var locationRetentionRate = locationExpired.Count == 0
                    ? 0m
                    : decimal.Round((decimal)locationRenewed / locationExpired.Count * 100m, 1, MidpointRounding.AwayFromZero);
                var locationChurnRate = locationExpired.Count == 0
                    ? 0m
                    : decimal.Round((decimal)locationChurned / locationExpired.Count * 100m, 1, MidpointRounding.AwayFromZero);

                return new GymRetentionLocationSummaryResponse(
                    location.Id,
                    location.Name,
                    locationActiveMembers.Count,
                    locationExpiring.Count,
                    locationExpiring.Count(item => !item.HasRenewalSale),
                    locationRenewed,
                    locationChurned,
                    locationRetentionRate,
                    locationChurnRate);
            })
            .ToList();

        var riskMembers = atRiskMembers
            .Take(12)
            .Select(item =>
            {
                var endsAtUtc = item.Membership.EndedAtUtc;
                var daysUntilExpiry = endsAtUtc.HasValue
                    ? Math.Max(0, (endsAtUtc.Value.Date - startOfToday).Days)
                    : 0;

                return new GymRetentionRiskMemberResponse(
                    item.Membership.Id,
                    item.ReportLocation!.Id,
                    item.ReportLocation.Name,
                    BuildMembershipDisplayName(item.Membership),
                    item.Membership.User?.Email?.Trim()
                        ?? item.Membership.InvitationEmail?.Trim()
                        ?? "Nessuna email",
                    item.Membership.Status,
                    endsAtUtc,
                    item.LastAccessAtUtc,
                    daysUntilExpiry,
                    item.HasRenewalSale);
            })
            .ToList();

        return Success(new GymRetentionChurnResponse(
            activeMembers.Count,
            expiringNext30Days.Count,
            atRiskMembers.Count,
            renewedLast90Days,
            churnedLast90Days,
            retentionRate,
            churnRate,
            locationSummaries,
            riskMembers,
            now));
    }

    [HttpGet("exports")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    public async Task<ActionResult<ApiResponse<GymReportExportCatalogResponse>>> GetAvailableExports(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymReportExportCatalogResponse>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<GymReportExportCatalogResponse>("Gym not found.");
        }

        var reportsScope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Reports,
            PermissionActions.Read,
            cancellationToken);

        if (!reportsScope.HasAnyAccess)
        {
            return ForbiddenError<GymReportExportCatalogResponse>();
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, reportsScope, locationId, cancellationToken);
        if (allowedLocationIds.Count == 0)
        {
            return Success(new GymReportExportCatalogResponse([], DateTime.UtcNow));
        }

        var now = DateTime.UtcNow;
        var gymName = await _dbContext.Gyms
            .AsNoTracking()
            .Where(gym => gym.Id == gymId)
            .Select(gym => gym.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(gymName))
        {
            return NotFoundError<GymReportExportCatalogResponse>("Gym not found.");
        }

        var revenueRows = await BuildRevenueByLocationExportRowsAsync(gymId, gymName, allowedLocationIds, now, cancellationToken);
        var atRiskRows = await BuildAtRiskMemberExportRowsAsync(gymId, allowedLocationIds, now, cancellationToken);
        var leadRows = await BuildLeadPipelineExportRowsAsync(gymId, allowedLocationIds, cancellationToken);

        return Success(new GymReportExportCatalogResponse(
            [
                new GymReportExportSummaryResponse("revenue-by-location", revenueRows.Count),
                new GymReportExportSummaryResponse("at-risk-members", atRiskRows.Count),
                new GymReportExportSummaryResponse("lead-pipeline", leadRows.Count)
            ],
            now));
    }

    [HttpGet("exports/{exportKey}/csv")]
    [Authorize(Policy = AuthorizationPolicies.ReportsRead)]
    public async Task<ActionResult> DownloadExportCsv(
        Guid gymId,
        string exportKey,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<object>().Result!;
        }

        var gymName = await _dbContext.Gyms
            .AsNoTracking()
            .Where(gym => gym.Id == gymId)
            .Select(gym => gym.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(gymName))
        {
            return NotFoundError<object>("Gym not found.").Result!;
        }

        var reportsScope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Reports,
            PermissionActions.Read,
            cancellationToken);

        if (!reportsScope.HasAnyAccess)
        {
            return ForbiddenError<object>().Result!;
        }

        var allowedLocationIds = await ResolveAllowedLocationIdsAsync(gymId, reportsScope, locationId, cancellationToken);
        if (allowedLocationIds.Count == 0)
        {
            return NotFoundError<object>("No visible data is available for export.").Result!;
        }

        var now = DateTime.UtcNow;
        var normalizedKey = exportKey.Trim().ToLowerInvariant();
        byte[] fileBytes;
        string fileName;

        switch (normalizedKey)
        {
            case "revenue-by-location":
            {
                var rows = await BuildRevenueByLocationExportRowsAsync(gymId, gymName, allowedLocationIds, now, cancellationToken);
                fileBytes = BuildCsvFileBytes(
                    ["location_id", "location_name", "sales_count_month", "revenue_month_amount", "pending_collections_amount"],
                    rows.Select(row => new[]
                    {
                        row.LocationId.ToString(),
                        row.LocationName,
                        row.SalesCountMonth.ToString(CultureInfo.InvariantCulture),
                        FormatCsvDecimal(row.RevenueMonthAmount),
                        FormatCsvDecimal(row.PendingCollectionsAmount)
                    }));
                fileName = BuildExportFileName(normalizedKey, now);
                break;
            }
            case "at-risk-members":
            {
                var rows = await BuildAtRiskMemberExportRowsAsync(gymId, allowedLocationIds, now, cancellationToken);
                fileBytes = BuildCsvFileBytes(
                    ["membership_id", "location_id", "location_name", "member_name", "member_email", "membership_status", "membership_ends_at_utc", "days_until_expiry", "last_access_at_utc", "has_renewal_sale"],
                    rows.Select(row => new[]
                    {
                        row.MembershipId.ToString(),
                        row.LocationId?.ToString() ?? string.Empty,
                        row.LocationName,
                        row.MemberName,
                        row.MemberEmail,
                        row.Status.ToString(),
                        FormatCsvDate(row.MembershipEndsAtUtc),
                        row.DaysUntilExpiry.ToString(CultureInfo.InvariantCulture),
                        FormatCsvDate(row.LastAccessAtUtc),
                        row.HasRenewalSale ? "true" : "false"
                    }));
                fileName = BuildExportFileName(normalizedKey, now);
                break;
            }
            case "lead-pipeline":
            {
                var rows = await BuildLeadPipelineExportRowsAsync(gymId, allowedLocationIds, cancellationToken);
                fileBytes = BuildCsvFileBytes(
                    ["lead_id", "location_id", "location_name", "full_name", "email", "phone_number", "stage", "source", "owner_name", "interest", "next_follow_up_at_utc", "is_follow_up_due", "open_tasks_count", "next_open_task_due_at_utc", "created_at_utc", "updated_at_utc"],
                    rows.Select(row => new[]
                    {
                        row.LeadId.ToString(),
                        row.LocationId.ToString(),
                        row.LocationName,
                        row.FullName,
                        row.Email,
                        row.PhoneNumber,
                        row.Stage.ToString(),
                        row.Source.ToString(),
                        row.OwnerName,
                        row.Interest,
                        FormatCsvDate(row.NextFollowUpAtUtc),
                        row.IsFollowUpDue ? "true" : "false",
                        row.OpenTasksCount.ToString(CultureInfo.InvariantCulture),
                        FormatCsvDate(row.NextOpenTaskDueAtUtc),
                        FormatCsvDate(row.CreatedAtUtc),
                        FormatCsvDate(row.UpdatedAtUtc)
                    }));
                fileName = BuildExportFileName(normalizedKey, now);
                break;
            }
            default:
                return NotFoundError<object>("Report export not found.").Result!;
        }

        return File(fileBytes, "text/csv; charset=utf-8", fileName);
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

    private static IReadOnlyCollection<GymKpiLeadStageResponse> BuildLeadPipeline(IEnumerable<GymLead> leads)
    {
        var leadList = leads.ToList();
        return Enum.GetValues<GymLeadStage>()
            .Select(stage => new GymKpiLeadStageResponse(stage, leadList.Count(lead => lead.Stage == stage)))
            .ToList();
    }

    private async Task<RetentionDataset> LoadRetentionDatasetAsync(
        Guid gymId,
        HashSet<Guid> allowedLocationIds,
        DateTime last90Days,
        CancellationToken cancellationToken)
    {
        var locations = await _dbContext.GymLocations
            .AsNoTracking()
            .Where(location => location.GymId == gymId && allowedLocationIds.Contains(location.Id))
            .OrderBy(location => location.Name)
            .ToListAsync(cancellationToken);

        var memberships = await _dbContext.GymMemberships
            .AsNoTracking()
            .Where(membership => membership.GymId == gymId && membership.Status != GymMembershipStatus.PendingClaim)
            .Include(membership => membership.User)
            .Include(membership => membership.MemberProfile)
            .Include(membership => membership.PrimaryLocation)
            .Include(membership => membership.Locations)
            .ToListAsync(cancellationToken);

        var visibleMemberships = memberships
            .Where(membership =>
                membership.PrimaryLocationId is { } primaryLocationId && allowedLocationIds.Contains(primaryLocationId)
                || membership.Locations.Any(location => allowedLocationIds.Contains(location.LocationId)))
            .ToList();

        var visibleMembershipIds = visibleMemberships.Select(membership => membership.Id).ToHashSet();
        var sales = await _dbContext.GymSales
            .AsNoTracking()
            .Where(sale => sale.GymId == gymId && sale.GymMembershipId != Guid.Empty && visibleMembershipIds.Contains(sale.GymMembershipId))
            .Include(sale => sale.Lines)
            .ToListAsync(cancellationToken);

        var activeSales = sales
            .Where(sale => sale.Status is not (GymSaleStatus.Cancelled or GymSaleStatus.Refunded))
            .ToList();

        var accessEvents = await _dbContext.GymAccessEvents
            .AsNoTracking()
            .Where(accessEvent =>
                accessEvent.GymId == gymId
                && accessEvent.GymMembershipId != Guid.Empty
                && visibleMembershipIds.Contains(accessEvent.GymMembershipId)
                && accessEvent.Result == GymAccessEventResult.Granted
                && accessEvent.OccurredAtUtc >= last90Days)
            .ToListAsync(cancellationToken);

        var lastAccessByMembershipId = accessEvents
            .GroupBy(accessEvent => accessEvent.GymMembershipId)
            .ToDictionary(
                group => group.Key,
                group => group.Max(accessEvent => accessEvent.OccurredAtUtc));

        var analyzedMemberships = visibleMemberships
            .Select(membership =>
            {
                var reportLocation = ResolveMembershipReportLocation(membership, locations);
                var qualifyingRenewalSale = membership.EndedAtUtc.HasValue
                    ? activeSales
                        .Where(sale => sale.GymMembershipId == membership.Id)
                        .Where(sale => sale.Lines.Any(line =>
                            line.ItemType is GymSaleItemType.MembershipPeriodic or GymSaleItemType.MembershipEntries))
                        .Where(sale =>
                            sale.SoldAtUtc >= membership.EndedAtUtc.Value.AddDays(-30)
                            && sale.SoldAtUtc <= membership.EndedAtUtc.Value.AddDays(30))
                        .OrderByDescending(sale => sale.SoldAtUtc)
                        .FirstOrDefault()
                    : null;

                lastAccessByMembershipId.TryGetValue(membership.Id, out var lastAccessAtUtc);
                return new RetentionMembershipSnapshot(
                    membership,
                    reportLocation,
                    qualifyingRenewalSale is not null,
                    qualifyingRenewalSale?.SoldAtUtc,
                    lastAccessAtUtc == default ? null : lastAccessAtUtc);
            })
            .Where(item => item.ReportLocation is not null)
            .ToList();

        return new RetentionDataset(locations, analyzedMemberships);
    }

    private async Task<List<RevenueByLocationExportRow>> BuildRevenueByLocationExportRowsAsync(
        Guid gymId,
        string gymName,
        HashSet<Guid> allowedLocationIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var locations = await _dbContext.GymLocations
            .AsNoTracking()
            .Where(location => location.GymId == gymId && allowedLocationIds.Contains(location.Id))
            .OrderBy(location => location.Name)
            .ToListAsync(cancellationToken);

        var sales = await _dbContext.GymSales
            .AsNoTracking()
            .Where(sale => sale.GymId == gymId && allowedLocationIds.Contains(sale.LocationId))
            .ToListAsync(cancellationToken);

        var activeSales = sales
            .Where(sale => sale.Status is not (GymSaleStatus.Cancelled or GymSaleStatus.Refunded))
            .ToList();

        return locations
            .Select(location =>
            {
                var locationSales = activeSales.Where(sale => sale.LocationId == location.Id).ToList();
                var monthSales = locationSales.Where(sale => sale.SoldAtUtc >= startOfMonth).ToList();
                var salesCountMonth = monthSales.Count;
                var revenueMonthAmount = monthSales.Sum(sale => sale.PaidAmount);
                var pendingCollectionsAmount = locationSales.Sum(sale => sale.RemainingAmount);

                return IsTenantPlaceholderRevenueLocation(
                    location,
                    gymName,
                    salesCountMonth,
                    revenueMonthAmount,
                    pendingCollectionsAmount)
                    ? null
                    : new RevenueByLocationExportRow(
                        location.Id,
                        location.Name,
                        salesCountMonth,
                        revenueMonthAmount,
                        pendingCollectionsAmount);
            })
            .OfType<RevenueByLocationExportRow>()
            .ToList();
    }

    private async Task<List<AtRiskMemberExportRow>> BuildAtRiskMemberExportRowsAsync(
        Guid gymId,
        HashSet<Guid> allowedLocationIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var startOfToday = now.Date;
        var next30Days = startOfToday.AddDays(30);
        var last90Days = startOfToday.AddDays(-90);
        var retentionDataset = await LoadRetentionDatasetAsync(gymId, allowedLocationIds, last90Days, cancellationToken);

        return retentionDataset.Memberships
            .Where(item => item.Membership.Status == GymMembershipStatus.Active)
            .Where(item =>
                item.Membership.EndedAtUtc.HasValue
                && item.Membership.EndedAtUtc.Value >= startOfToday
                && item.Membership.EndedAtUtc.Value <= next30Days
                && !item.HasRenewalSale)
            .OrderBy(item => item.Membership.EndedAtUtc)
            .ThenBy(item => BuildMembershipDisplayName(item.Membership))
            .Select(item =>
            {
                var endsAtUtc = item.Membership.EndedAtUtc;
                var daysUntilExpiry = endsAtUtc.HasValue
                    ? Math.Max(0, (endsAtUtc.Value.Date - startOfToday).Days)
                    : 0;

                return new AtRiskMemberExportRow(
                    item.Membership.Id,
                    item.ReportLocation!.Id,
                    item.ReportLocation.Name,
                    BuildMembershipDisplayName(item.Membership),
                    item.Membership.User?.Email?.Trim()
                        ?? item.Membership.InvitationEmail?.Trim()
                        ?? string.Empty,
                    item.Membership.Status,
                    endsAtUtc,
                    item.LastAccessAtUtc,
                    daysUntilExpiry,
                    item.HasRenewalSale);
            })
            .ToList();
    }

    private async Task<List<LeadPipelineExportRow>> BuildLeadPipelineExportRowsAsync(
        Guid gymId,
        HashSet<Guid> allowedLocationIds,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var leads = await _dbContext.GymLeads
            .AsNoTracking()
            .Where(lead =>
                lead.GymId == gymId
                && allowedLocationIds.Contains(lead.LocationId)
                && lead.Stage != GymLeadStage.Won
                && lead.Stage != GymLeadStage.Lost)
            .Include(lead => lead.Location)
            .Include(lead => lead.OwnerAssignment)
                .ThenInclude(assignment => assignment!.StaffProfile)
            .Include(lead => lead.OwnerAssignment)
                .ThenInclude(assignment => assignment!.User)
            .Include(lead => lead.Tasks)
            .OrderBy(lead => lead.NextFollowUpAtUtc ?? DateTime.MaxValue)
            .ThenBy(lead => lead.FullName)
            .ToListAsync(cancellationToken);

        return leads
            .Select(lead =>
            {
                var openTasks = lead.Tasks
                    .Where(task => task.Status == GymLeadTaskStatus.Open)
                    .ToList();

                return new LeadPipelineExportRow(
                    lead.Id,
                    lead.LocationId,
                    lead.Location.Name,
                    lead.FullName,
                    lead.Email?.Trim() ?? string.Empty,
                    lead.PhoneNumber?.Trim() ?? string.Empty,
                    lead.Stage,
                    lead.Source,
                    ResolveOwnerDisplayName(lead.OwnerAssignment),
                    lead.Interest?.Trim() ?? string.Empty,
                    lead.NextFollowUpAtUtc,
                    lead.NextFollowUpAtUtc.HasValue && lead.NextFollowUpAtUtc.Value <= now,
                    openTasks.Count,
                    openTasks
                        .Where(task => task.DueAtUtc.HasValue)
                        .OrderBy(task => task.DueAtUtc)
                        .Select(task => task.DueAtUtc)
                        .FirstOrDefault(),
                    lead.CreatedAtUtc,
                    lead.UpdatedAtUtc);
            })
            .ToList();
    }

    private static string ResolveOwnerDisplayName(TenantRoleAssignment? assignment)
    {
        if (assignment is null)
        {
            return string.Empty;
        }

        var displayName = assignment.StaffProfile?.DisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName;
        }

        var fullName = assignment.User?.FullName?.Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return assignment.User?.Email?.Trim() ?? string.Empty;
    }

    private static bool IsTenantPlaceholderRevenueLocation(
        GymLocation location,
        string gymName,
        int salesCountMonth,
        decimal revenueMonthAmount,
        decimal pendingCollectionsAmount)
    {
        return salesCountMonth == 0
            && revenueMonthAmount == 0m
            && pendingCollectionsAmount == 0m
            && string.Equals(location.Name.Trim(), gymName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] BuildCsvFileBytes(
        IReadOnlyList<string> headers,
        IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(';', headers.Select(EscapeCsvValue)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(';', row.Select(EscapeCsvValue)));
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(builder.ToString())];
    }

    private static string EscapeCsvValue(string? value)
    {
        var safeValue = value?.Replace("\"", "\"\"") ?? string.Empty;
        return $"\"{safeValue}\"";
    }

    private static string FormatCsvDate(DateTime? value)
    {
        return value?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatCsvDecimal(decimal value)
    {
        return value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string BuildExportFileName(string exportKey, DateTime generatedAtUtc)
    {
        return $"betterfit-{exportKey}-{generatedAtUtc:yyyyMMdd-HHmmss}.csv";
    }

    private static GymLocation? ResolveMembershipReportLocation(
        GymMembership membership,
        IReadOnlyCollection<GymLocation> visibleLocations)
    {
        if (membership.PrimaryLocationId is { } primaryLocationId)
        {
            var primaryLocation = visibleLocations.FirstOrDefault(location => location.Id == primaryLocationId);
            if (primaryLocation is not null)
            {
                return primaryLocation;
            }
        }

        var allowedLocationIds = membership.Locations
            .Select(location => location.LocationId)
            .ToHashSet();

        return visibleLocations.FirstOrDefault(location => allowedLocationIds.Contains(location.Id));
    }

    private static string BuildMembershipDisplayName(GymMembership membership)
    {
        var profileName = $"{membership.MemberProfile?.FirstName} {membership.MemberProfile?.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(profileName))
        {
            return profileName;
        }

        var pendingName = $"{membership.PendingFirstName} {membership.PendingLastName}".Trim();
        if (!string.IsNullOrWhiteSpace(pendingName))
        {
            return pendingName;
        }

        return membership.User?.FullName?.Trim()
            ?? membership.User?.Email?.Trim()
            ?? membership.InvitationEmail?.Trim()
            ?? "Membro Betterfit";
    }

    private sealed record RetentionMembershipSnapshot(
        GymMembership Membership,
        GymLocation? ReportLocation,
        bool HasRenewalSale,
        DateTime? RenewalSaleAtUtc,
        DateTime? LastAccessAtUtc);

    private sealed record RetentionDataset(
        IReadOnlyCollection<GymLocation> Locations,
        IReadOnlyCollection<RetentionMembershipSnapshot> Memberships);

    private sealed record RevenueByLocationExportRow(
        Guid LocationId,
        string LocationName,
        int SalesCountMonth,
        decimal RevenueMonthAmount,
        decimal PendingCollectionsAmount);

    private sealed record AtRiskMemberExportRow(
        Guid MembershipId,
        Guid? LocationId,
        string LocationName,
        string MemberName,
        string MemberEmail,
        GymMembershipStatus Status,
        DateTime? MembershipEndsAtUtc,
        DateTime? LastAccessAtUtc,
        int DaysUntilExpiry,
        bool HasRenewalSale);

    private sealed record LeadPipelineExportRow(
        Guid LeadId,
        Guid LocationId,
        string LocationName,
        string FullName,
        string Email,
        string PhoneNumber,
        GymLeadStage Stage,
        GymLeadSource Source,
        string OwnerName,
        string Interest,
        DateTime? NextFollowUpAtUtc,
        bool IsFollowUpDue,
        int OpenTasksCount,
        DateTime? NextOpenTaskDueAtUtc,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc);
}
