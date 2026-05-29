using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Dashboard;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<GymDashboardOverviewResponse>>> GetOverview(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymDashboardOverviewResponse>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<GymDashboardOverviewResponse>("Gym not found.");
        }

        var gymLocations = await _dbContext.GymLocations
            .AsNoTracking()
            .Where(location => location.GymId == gymId && location.IsActive)
            .OrderBy(location => location.Name)
            .ToListAsync(cancellationToken);

        var membersScope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Members,
            PermissionActions.Read,
            cancellationToken);
        var billingScope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Read,
            cancellationToken);
        var checkinsScope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Checkins,
            PermissionActions.Approve,
            cancellationToken);
        var integrationsScope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Integrations,
            PermissionActions.Read,
            cancellationToken);

        if (!membersScope.HasAnyAccess && !billingScope.HasAnyAccess && !checkinsScope.HasAnyAccess)
        {
            return ForbiddenError<GymDashboardOverviewResponse>();
        }

        var visibleLocationIds = gymLocations
            .Where(location =>
                membersScope.CanAccessLocation(location.Id)
                || billingScope.CanAccessLocation(location.Id)
                || checkinsScope.CanAccessLocation(location.Id))
            .Select(location => location.Id)
            .ToHashSet();

        if (locationId.HasValue && !visibleLocationIds.Contains(locationId.Value))
        {
            return ForbiddenError<GymDashboardOverviewResponse>(
                "The selected location is outside your current dashboard scope.");
        }

        var dashboardLocationIds = locationId.HasValue
            ? new HashSet<Guid> { locationId.Value }
            : visibleLocationIds;
        var dashboardLocations = gymLocations
            .Where(location => dashboardLocationIds.Contains(location.Id))
            .ToList();

        var now = DateTime.UtcNow;
        var startOfToday = now.Date;
        var last30Days = startOfToday.AddDays(-30);
        var renewalWindowEnd = startOfToday.AddDays(30);

        List<GymMembership> visibleMemberships = [];
        if (membersScope.HasAnyAccess)
        {
            var memberships = await _dbContext.GymMemberships
                .AsNoTracking()
                .Where(membership => membership.GymId == gymId)
                .Include(membership => membership.User)
                .Include(membership => membership.MemberProfile)
                .Include(membership => membership.Locations)
                    .ThenInclude(location => location.Location)
                .ToListAsync(cancellationToken);

            visibleMemberships = memberships
                .Where(membership => MembershipMatchesScope(membership, membersScope, dashboardLocationIds))
                .ToList();
        }

        List<GymSale> visibleSales = [];
        if (billingScope.HasAnyAccess)
        {
            var sales = await _dbContext.GymSales
                .AsNoTracking()
                .Where(sale => sale.GymId == gymId)
                .Include(sale => sale.Location)
                .Include(sale => sale.Membership)
                    .ThenInclude(membership => membership.User)
                .Include(sale => sale.Membership)
                    .ThenInclude(membership => membership.MemberProfile)
                .OrderByDescending(sale => sale.SoldAtUtc)
                .ToListAsync(cancellationToken);

            visibleSales = sales
                .Where(sale => dashboardLocationIds.Contains(sale.LocationId) && billingScope.CanAccessLocation(sale.LocationId))
                .ToList();
        }

        List<GymDashboardRecentCollectionResponse> recentCollections = [];
        if (billingScope.HasAnyAccess)
        {
            var paidPayments = await _dbContext.GymSalePayments
                .AsNoTracking()
                .Where(payment =>
                    payment.Status == GymSalePaymentStatus.Paid
                    && payment.PaidAtUtc.HasValue
                    && payment.Sale.GymId == gymId
                    && dashboardLocationIds.Contains(payment.Sale.LocationId)
                    && payment.Sale.Status != GymSaleStatus.Cancelled
                    && payment.Sale.Status != GymSaleStatus.Refunded)
                .OrderByDescending(payment => payment.PaidAtUtc)
                .ThenByDescending(payment => payment.CreatedAtUtc)
                .Select(payment => new
                {
                    payment.Id,
                    PaidAtUtc = payment.PaidAtUtc!.Value,
                    payment.Amount,
                    payment.ReceiptCode,
                    payment.Method,
                    payment.Sale.ReferenceCode,
                    MemberFirstName = payment.Sale.Membership.MemberProfile != null
                        ? payment.Sale.Membership.MemberProfile.FirstName
                        : null,
                    MemberLastName = payment.Sale.Membership.MemberProfile != null
                        ? payment.Sale.Membership.MemberProfile.LastName
                        : null,
                    MemberEmail = payment.Sale.Membership.User != null
                        ? payment.Sale.Membership.User.Email
                        : null,
                    payment.Sale.Membership.InvitationEmail,
                    LocationName = payment.Sale.Location.Name
                })
                .Take(6)
                .ToListAsync(cancellationToken);

            recentCollections = paidPayments
                .Select(payment => new GymDashboardRecentCollectionResponse(
                    payment.Id,
                    payment.PaidAtUtc,
                    payment.ReferenceCode,
                    ResolveReceiptCode(payment.ReferenceCode, payment.Id, payment.ReceiptCode),
                    MembershipDisplayName(
                        payment.MemberFirstName,
                        payment.MemberLastName,
                        payment.MemberEmail,
                        payment.InvitationEmail),
                    payment.LocationName,
                    payment.Amount,
                    payment.Method.ToString()))
                .ToList();
        }

        List<GymAccessEvent> visibleAccessEvents = [];
        if (checkinsScope.HasAnyAccess)
        {
            var accessEvents = await _dbContext.GymAccessEvents
                .AsNoTracking()
                .Where(accessEvent => accessEvent.GymId == gymId && accessEvent.OccurredAtUtc >= last30Days)
                .Include(accessEvent => accessEvent.Location)
                .Include(accessEvent => accessEvent.Membership)
                    .ThenInclude(membership => membership.User)
                .Include(accessEvent => accessEvent.Membership)
                    .ThenInclude(membership => membership.MemberProfile)
                .OrderByDescending(accessEvent => accessEvent.OccurredAtUtc)
                .ToListAsync(cancellationToken);

            visibleAccessEvents = accessEvents
                .Where(accessEvent =>
                    dashboardLocationIds.Contains(accessEvent.LocationId)
                    && checkinsScope.CanAccessLocation(accessEvent.LocationId))
                .ToList();
        }

        List<GymIntegration> visibleIntegrations = [];
        if (integrationsScope.HasAnyAccess)
        {
            var integrations = await _dbContext.GymIntegrations
                .AsNoTracking()
                .Where(integration => integration.GymId == gymId)
                .Include(integration => integration.Location)
                .OrderBy(integration => integration.Type)
                .ThenBy(integration => integration.DisplayName)
                .ToListAsync(cancellationToken);

            visibleIntegrations = integrations
                .Where(integration =>
                    integration.LocationId.HasValue
                        ? dashboardLocationIds.Contains(integration.LocationId.Value)
                            && integrationsScope.CanAccessLocation(integration.LocationId.Value)
                        : integrationsScope.HasTenantWideAccess)
                .ToList();
        }

        var todaysGrantedEvents = visibleAccessEvents
            .Where(accessEvent =>
                accessEvent.Result == GymAccessEventResult.Granted
                && accessEvent.OccurredAtUtc >= startOfToday)
            .ToList();
        var grantedEventsLast30Days = visibleAccessEvents
            .Where(accessEvent =>
                accessEvent.Result == GymAccessEventResult.Granted
                && accessEvent.OccurredAtUtc >= last30Days)
            .ToList();
        var deniedTodayEvents = visibleAccessEvents
            .Where(accessEvent =>
                accessEvent.Result == GymAccessEventResult.Denied
                && accessEvent.OccurredAtUtc >= startOfToday)
            .ToList();

        var activeSales = visibleSales
            .Where(sale => sale.Status is not (GymSaleStatus.Cancelled or GymSaleStatus.Refunded))
            .ToList();
        var expiringMemberships = visibleMemberships
            .Where(membership =>
                membership.Status == GymMembershipStatus.Active
                && membership.EndedAtUtc is { } endedAtUtc
                && endedAtUtc >= startOfToday
                && endedAtUtc <= renewalWindowEnd)
            .OrderBy(membership => membership.EndedAtUtc)
            .ToList();
        var pendingActivations = visibleMemberships
            .Where(membership => membership.Status == GymMembershipStatus.PendingClaim)
            .OrderBy(membership => membership.CreatedAtUtc)
            .ToList();
        var failedPayments = activeSales
            .Where(sale => sale.PaymentStatus == GymSalePaymentStatus.Failed)
            .OrderByDescending(sale => sale.SoldAtUtc)
            .ToList();

        var tasks = BuildTasks(expiringMemberships, pendingActivations, failedPayments, deniedTodayEvents, now);
        var alerts = BuildAlerts(
            pendingCollectionsAmount: activeSales.Sum(sale => sale.RemainingAmount),
            deniedTodayCount: deniedTodayEvents.Count,
            failedPaymentsCount: failedPayments.Count,
            expiringMembershipsCount: expiringMemberships.Count,
            pendingActivationsCount: pendingActivations.Count,
            dashboardLocations.Count);
        var statusCards = BuildStatusCards(visibleMemberships, grantedEventsLast30Days, now);
        var devices = BuildDeviceSnapshots(dashboardLocations, visibleIntegrations);
        var timeline = BuildTimeline(visibleSales, visibleAccessEvents, pendingActivations);
        var locationSnapshots = dashboardLocations
            .Select(location => new GymDashboardLocationSnapshotResponse(
                location.Id,
                location.Name,
                todaysGrantedEvents.Count(accessEvent => accessEvent.LocationId == location.Id),
                activeSales
                    .Where(sale => sale.LocationId == location.Id && sale.SoldAtUtc >= startOfToday)
                    .Sum(sale => sale.PaidAmount),
                expiringMemberships.Count(membership => membership.Locations.Any(item => item.LocationId == location.Id)),
                pendingActivations.Count(membership => membership.Locations.Any(item => item.LocationId == location.Id))))
            .ToList();

        var overview = new GymDashboardOverviewResponse(
            todaysGrantedEvents.Count,
            activeSales
                .Where(sale => sale.SoldAtUtc >= startOfToday)
                .Sum(sale => sale.PaidAmount),
            expiringMemberships.Count,
            pendingActivations.Count,
            activeSales.Sum(sale => sale.RemainingAmount),
            deniedTodayEvents.Count,
            failedPayments.Count,
            membersScope.HasAnyAccess,
            billingScope.HasAnyAccess,
            checkinsScope.HasAnyAccess,
            tasks,
            alerts,
            statusCards,
            devices,
            timeline,
            locationSnapshots,
            recentCollections,
            now);

        return Success(overview);
    }

    private static bool MembershipMatchesScope(
        GymMembership membership,
        GymPermissionScope membersScope,
        IReadOnlySet<Guid> dashboardLocationIds)
    {
        return membership.Locations.Any(location =>
            dashboardLocationIds.Contains(location.LocationId)
            && membersScope.CanAccessLocation(location.LocationId));
    }

    private static List<GymDashboardTaskResponse> BuildTasks(
        IReadOnlyCollection<GymMembership> expiringMemberships,
        IReadOnlyCollection<GymMembership> pendingActivations,
        IReadOnlyCollection<GymSale> failedPayments,
        IReadOnlyCollection<GymAccessEvent> deniedTodayEvents,
        DateTime now)
    {
        var tasks = new List<(DateTime SortAtUtc, GymDashboardTaskResponse Task)>();

        foreach (var membership in expiringMemberships.Take(3))
        {
            tasks.Add((
                membership.EndedAtUtc ?? now,
                new GymDashboardTaskResponse(
                    "Contattare per rinnovo",
                    MembershipDisplayName(membership),
                    membership.EndedAtUtc <= now.AddDays(3) ? "urgent" : "pending",
                    membership.EndedAtUtc,
                    membership.EndedAtUtc <= now
                        ? "La membership risulta gia scaduta."
                        : $"Scadenza prevista il {membership.EndedAtUtc:dd/MM/yyyy}."))
            );
        }

        foreach (var membership in pendingActivations.Take(2))
        {
            tasks.Add((
                membership.CreatedAtUtc,
                new GymDashboardTaskResponse(
                    "Completare attivazione",
                    MembershipDisplayName(membership),
                    membership.CreatedAtUtc <= now.AddDays(-2) ? "urgent" : "pending",
                    membership.CreatedAtUtc.AddDays(2),
                    "Membership creata dal desk ma non ancora claimata."))
            );
        }

        foreach (var sale in failedPayments.Take(2))
        {
            tasks.Add((
                sale.SoldAtUtc,
                new GymDashboardTaskResponse(
                    "Recuperare pagamento fallito",
                    MembershipDisplayName(sale.Membership),
                    "urgent",
                    sale.SoldAtUtc.AddDays(1),
                    $"{sale.ReferenceCode} - residuo {sale.RemainingAmount:0.00} EUR."))
            );
        }

        foreach (var accessEvent in deniedTodayEvents
            .Where(accessEvent => !string.IsNullOrWhiteSpace(accessEvent.Reason))
            .Take(2))
        {
            tasks.Add((
                accessEvent.OccurredAtUtc,
                new GymDashboardTaskResponse(
                    "Verificare accesso negato",
                    MembershipDisplayName(accessEvent.Membership),
                    "pending",
                    accessEvent.OccurredAtUtc,
                    accessEvent.Reason ?? "Accesso negato da verificare al desk."))
            );
        }

        return tasks
            .OrderBy(task => task.Task.Status == "urgent" ? 0 : 1)
            .ThenBy(task => task.SortAtUtc)
            .Take(6)
            .Select(task => task.Task)
            .ToList();
    }

    private static List<GymDashboardAlertResponse> BuildAlerts(
        decimal pendingCollectionsAmount,
        int deniedTodayCount,
        int failedPaymentsCount,
        int expiringMembershipsCount,
        int pendingActivationsCount,
        int locationsCount)
    {
        var alerts = new List<GymDashboardAlertResponse>();

        if (failedPaymentsCount > 0)
        {
            alerts.Add(new GymDashboardAlertResponse(
                "Pagamenti falliti da gestire",
                $"{failedPaymentsCount} vendite risultano con pagamento fallito o da recuperare.",
                "critical"));
        }

        if (pendingCollectionsAmount > 0)
        {
            alerts.Add(new GymDashboardAlertResponse(
                "Incassi non ancora chiusi",
                $"Ci sono {pendingCollectionsAmount:0.00} EUR ancora da incassare sulle vendite attive.",
                pendingCollectionsAmount >= 100 ? "warning" : "ok"));
        }

        if (deniedTodayCount > 0)
        {
            alerts.Add(new GymDashboardAlertResponse(
                "Accessi negati oggi",
                $"{deniedTodayCount} tentativi respinti richiedono eventuale verifica manuale.",
                deniedTodayCount >= 3 ? "warning" : "ok"));
        }

        if (expiringMembershipsCount > 0)
        {
            alerts.Add(new GymDashboardAlertResponse(
                "Rinnovi in finestra calda",
                $"{expiringMembershipsCount} membership scadono entro i prossimi 30 giorni.",
                expiringMembershipsCount >= 5 ? "warning" : "ok"));
        }

        if (pendingActivationsCount > 0)
        {
            alerts.Add(new GymDashboardAlertResponse(
                "Attivazioni non concluse",
                $"{pendingActivationsCount} clienti risultano preregistrati ma non ancora attivati.",
                pendingActivationsCount >= 3 ? "warning" : "ok"));
        }

        if (locationsCount == 0)
        {
            alerts.Add(new GymDashboardAlertResponse(
                "Nessuna sede disponibile",
                "La dashboard non ha sedi operative visibili nel perimetro corrente.",
                "critical"));
        }

        return alerts.Take(5).ToList();
    }

    private static List<GymDashboardTimelineItemResponse> BuildTimeline(
        IReadOnlyCollection<GymSale> sales,
        IReadOnlyCollection<GymAccessEvent> accessEvents,
        IReadOnlyCollection<GymMembership> pendingActivations)
    {
        var timeline = new List<GymDashboardTimelineItemResponse>();

        timeline.AddRange(sales.Take(6).Select(sale => new GymDashboardTimelineItemResponse(
            sale.SoldAtUtc,
            $"Vendita {sale.ReferenceCode} registrata per {MembershipDisplayName(sale.Membership)} ({sale.PaidAmount:0.00} EUR incassati).")));

        timeline.AddRange(accessEvents.Take(8).Select(accessEvent => new GymDashboardTimelineItemResponse(
            accessEvent.OccurredAtUtc,
            accessEvent.Result == GymAccessEventResult.Granted
                ? $"Check-in consentito a {MembershipDisplayName(accessEvent.Membership)} presso {accessEvent.Location.Name}."
                : $"Accesso negato a {MembershipDisplayName(accessEvent.Membership)}: {accessEvent.Reason ?? "motivo non disponibile"}.")));

        timeline.AddRange(pendingActivations.Take(4).Select(membership => new GymDashboardTimelineItemResponse(
            membership.CreatedAtUtc,
            $"Nuova preregistrazione desk per {MembershipDisplayName(membership)}.")));

        return timeline
            .OrderByDescending(item => item.OccurredAtUtc)
            .Take(10)
            .ToList();
    }

    private static List<GymDashboardStatusCardResponse> BuildStatusCards(
        IReadOnlyCollection<GymMembership> memberships,
        IReadOnlyCollection<GymAccessEvent> grantedEventsLast30Days,
        DateTime now)
    {
        var activeMemberships = memberships
            .Where(membership => membership.Status == GymMembershipStatus.Active)
            .ToList();
        var accessCountByMembership = grantedEventsLast30Days
            .GroupBy(accessEvent => accessEvent.GymMembershipId)
            .ToDictionary(group => group.Key, group => group.Count());

        var noAccessUsers = activeMemberships
            .Count(membership => !accessCountByMembership.ContainsKey(membership.Id));
        var lowAccessUsers = activeMemberships
            .Count(membership =>
                accessCountByMembership.TryGetValue(membership.Id, out var accessCount)
                && accessCount is > 0 and <= 2);
        var expiringToday = activeMemberships
            .Count(membership =>
                membership.EndedAtUtc.HasValue
                && membership.EndedAtUtc.Value.Date <= now.Date);

        return
        [
            new GymDashboardStatusCardResponse(
                "profiles",
                "Profili nel perimetro",
                memberships.Count.ToString(),
                "Membership visibili nel tenant o nella sede selezionata.",
                "neutral"),
            new GymDashboardStatusCardResponse(
                "activeUsers",
                "Membri attivi",
                activeMemberships.Count.ToString(),
                "Clienti con membership attiva in questo momento.",
                activeMemberships.Count > 0 ? "success" : "warning"),
            new GymDashboardStatusCardResponse(
                "lowAccessUsers",
                "Accesso basso 30 giorni",
                lowAccessUsers.ToString(),
                "Membri attivi con al massimo due accessi negli ultimi 30 giorni.",
                lowAccessUsers > 0 ? "warning" : "success"),
            new GymDashboardStatusCardResponse(
                "noAccessUsers",
                "Nessun accesso 30 giorni",
                noAccessUsers.ToString(),
                "Membri attivi senza ingressi registrati negli ultimi 30 giorni.",
                noAccessUsers > 0 ? "danger" : "success"),
            new GymDashboardStatusCardResponse(
                "expiringToday",
                "Scadenze oggi",
                expiringToday.ToString(),
                "Membership in scadenza oggi o gia scadute da gestire in desk.",
                expiringToday > 0 ? "warning" : "success")
        ];
    }

    private static List<GymDashboardDeviceResponse> BuildDeviceSnapshots(
        IReadOnlyCollection<GymLocation> dashboardLocations,
        IReadOnlyCollection<GymIntegration> integrations)
    {
        var devices = new List<GymDashboardDeviceResponse>();

        foreach (var location in dashboardLocations.OrderBy(location => location.Name).Take(3))
        {
            var accessControl = integrations
                .FirstOrDefault(integration =>
                    integration.Type == GymIntegrationType.AccessControl
                    && integration.LocationId == location.Id);

            if (accessControl is null)
            {
                devices.Add(new GymDashboardDeviceResponse(
                    $"Accessi {location.Code ?? location.Name}",
                    "maintenance",
                    "Nessuna integrazione accessi configurata per questa sede."));
                continue;
            }

            devices.Add(MapIntegrationDevice(accessControl, $"Accessi {location.Code ?? location.Name}"));
        }

        foreach (var integration in integrations
            .Where(integration =>
                integration.LocationId is null
                && integration.Type is GymIntegrationType.EmailDelivery or GymIntegrationType.WhatsAppMessaging or GymIntegrationType.AccountingExport))
        {
            devices.Add(MapIntegrationDevice(integration, integration.DisplayName));
        }

        return devices.Take(6).ToList();
    }

    private static GymDashboardDeviceResponse MapIntegrationDevice(GymIntegration integration, string fallbackName)
    {
        var status = integration.Status switch
        {
            GymIntegrationStatus.Disabled => "offline",
            GymIntegrationStatus.Draft => "maintenance",
            _ when integration.LastSyncSucceeded == false => "offline",
            _ => "online"
        };

        var lastEvent = integration.LastSyncAttemptAtUtc.HasValue
            ? $"Ultimo test {integration.LastSyncAttemptAtUtc.Value:dd/MM HH:mm} - {integration.LastSyncMessage ?? "stato disponibile"}"
            : integration.Status == GymIntegrationStatus.Draft
                ? "Configurazione in bozza, test operativo non ancora eseguito."
                : "Nessun test operativo registrato.";

        return new GymDashboardDeviceResponse(
            string.IsNullOrWhiteSpace(integration.DisplayName) ? fallbackName : integration.DisplayName,
            status,
            lastEvent);
    }

    private static string MembershipDisplayName(GymMembership membership)
    {
        return MembershipDisplayName(
            membership.MemberProfile?.FirstName ?? membership.PendingFirstName,
            membership.MemberProfile?.LastName ?? membership.PendingLastName,
            membership.User?.Email,
            membership.InvitationEmail);
    }

    private static string MembershipDisplayName(
        string? firstName,
        string? lastName,
        string? userEmail,
        string? invitationEmail)
    {
        var normalizedFirstName = firstName?.Trim();
        var normalizedLastName = lastName?.Trim();
        var fullName = string.Join(
            " ",
            new[] { normalizedFirstName, normalizedLastName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return userEmail?.Trim()
            ?? invitationEmail?.Trim()
            ?? "Cliente senza nome";
    }

    private static string ResolveReceiptCode(string referenceCode, Guid paymentId, string? receiptCode)
    {
        if (!string.IsNullOrWhiteSpace(receiptCode))
        {
            return receiptCode.Trim();
        }

        return $"RIC-{referenceCode}-{paymentId.ToString("N")[..6].ToUpperInvariant()}";
    }
}
