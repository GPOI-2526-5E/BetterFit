using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Sales;
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
[Route("api/gyms/{gymId:guid}/sales")]
public sealed class SalesController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SalesController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet("overview")]
    [Authorize(Policy = AuthorizationPolicies.BillingRead)]
    public async Task<ActionResult<ApiResponse<GymSalesOverviewResponse>>> GetOverview(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSalesOverviewResponse>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<GymSalesOverviewResponse>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSalesOverviewResponse>();
        }

        var startOfToday = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(startOfToday.Year, startOfToday.Month, 1);
        var renewalWindowEnd = startOfToday.AddDays(30);

        var sales = await _dbContext.GymSales
            .AsNoTracking()
            .Where(sale => sale.GymId == gymId)
            .Include(sale => sale.Lines)
            .ToListAsync(cancellationToken);

        var visibleSales = sales
            .Where(sale => scope.CanAccessLocation(sale.LocationId))
            .ToList();
        var activeSales = visibleSales
            .Where(sale => sale.Status is not (GymSaleStatus.Cancelled or GymSaleStatus.Refunded))
            .ToList();

        var memberships = await _dbContext.GymMemberships
            .AsNoTracking()
            .Where(membership => membership.GymId == gymId && membership.Status == GymMembershipStatus.Active)
            .Include(membership => membership.Locations)
            .ToListAsync(cancellationToken);

        var renewalCandidatesCount = memberships.Count(membership =>
            membership.EndedAtUtc is { } endedAtUtc
            && endedAtUtc >= startOfToday
            && endedAtUtc <= renewalWindowEnd
            && membership.Locations.Any(location => scope.CanAccessLocation(location.LocationId)));

        var overview = new GymSalesOverviewResponse(
            activeSales.Count(sale => sale.SoldAtUtc >= startOfToday),
            activeSales
                .Where(sale => sale.SoldAtUtc >= startOfToday)
                .Sum(sale => sale.PaidAmount),
            activeSales
                .Where(sale => sale.SoldAtUtc >= startOfMonth)
                .Sum(sale => sale.PaidAmount),
            activeSales.Sum(sale => sale.RemainingAmount),
            renewalCandidatesCount,
            activeSales.Count(sale => sale.PaymentStatus == GymSalePaymentStatus.Failed));

        return Success(overview);
    }

    [HttpGet("catalog")]
    [Authorize(Policy = AuthorizationPolicies.BillingRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymSaleCatalogItemResponse>>>> GetCatalog(
        Guid gymId,
        [FromQuery] Guid? locationId,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymSaleCatalogItemResponse>>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<GymSaleCatalogItemResponse>>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymSaleCatalogItemResponse>>();
        }

        IQueryable<GymSaleCatalogItem> query = _dbContext.GymSaleCatalogItems
            .AsNoTracking()
            .Where(item => item.GymId == gymId)
            .Include(item => item.Location);

        if (locationId.HasValue)
        {
            query = query.Where(item => item.LocationId == locationId.Value);
        }

        if (!includeInactive)
        {
            query = query.Where(item => item.IsActive);
        }

        var items = await query
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Location.Name)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        var visibleItems = items
            .Where(item => scope.CanAccessLocation(item.LocationId))
            .Select(ResponseMappers.MapSaleCatalogItemResponse)
            .ToList();

        return Success<IReadOnlyCollection<GymSaleCatalogItemResponse>>(visibleItems);
    }

    [HttpPost("catalog")]
    [Authorize(Policy = AuthorizationPolicies.BillingWrite)]
    public async Task<ActionResult<ApiResponse<GymSaleCatalogItemResponse>>> CreateCatalogItem(
        Guid gymId,
        [FromBody] UpsertGymSaleCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleCatalogItemResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleCatalogItemResponse>();
        }

        var location = await _dbContext.GymLocations
            .SingleOrDefaultAsync(item => item.Id == request.LocationId && item.GymId == gymId, cancellationToken);
        if (location is null || !location.IsActive)
        {
            return BadRequestError<GymSaleCatalogItemResponse>("invalid_location", "The selected location does not belong to the gym.");
        }

        if (!scope.CanAccessLocation(location.Id))
        {
            return ForbiddenError<GymSaleCatalogItemResponse>("This catalog item belongs to a location outside your staff scope.");
        }

        if (!TryValidateCatalogItemRequest(request, out var errorCode, out var errorMessage))
        {
            return BadRequestError<GymSaleCatalogItemResponse>(errorCode!, errorMessage!);
        }

        var now = DateTime.UtcNow;
        var item = new GymSaleCatalogItem
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            LocationId = location.Id,
            ItemType = request.ItemType,
            Name = request.Name.Trim(),
            UnitPriceAmount = decimal.Round(request.UnitPriceAmount, 2, MidpointRounding.AwayFromZero),
            DefaultQuantity = request.DefaultQuantity,
            DefaultDiscountAmount = decimal.Round(request.DefaultDiscountAmount, 2, MidpointRounding.AwayFromZero),
            DefaultCreditsGranted = request.DefaultCreditsGranted,
            ServicePeriodDays = request.ServicePeriodDays,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymSaleCatalogItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        item = await _dbContext.GymSaleCatalogItems
            .AsNoTracking()
            .Include(entry => entry.Location)
            .SingleAsync(entry => entry.Id == item.Id, cancellationToken);

        return CreatedAt(
            nameof(GetCatalog),
            new { gymId },
            ResponseMappers.MapSaleCatalogItemResponse(item));
    }

    [HttpPut("catalog/{catalogItemId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.BillingWrite)]
    public async Task<ActionResult<ApiResponse<GymSaleCatalogItemResponse>>> UpdateCatalogItem(
        Guid gymId,
        Guid catalogItemId,
        [FromBody] UpsertGymSaleCatalogItemRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleCatalogItemResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleCatalogItemResponse>();
        }

        var item = await _dbContext.GymSaleCatalogItems
            .Include(entry => entry.Location)
            .SingleOrDefaultAsync(entry => entry.Id == catalogItemId && entry.GymId == gymId, cancellationToken);
        if (item is null)
        {
            return NotFoundError<GymSaleCatalogItemResponse>("Catalog item not found.");
        }

        if (!scope.CanAccessLocation(item.LocationId))
        {
            return ForbiddenError<GymSaleCatalogItemResponse>("This catalog item belongs to a location outside your staff scope.");
        }

        var location = await _dbContext.GymLocations
            .SingleOrDefaultAsync(entry => entry.Id == request.LocationId && entry.GymId == gymId, cancellationToken);
        if (location is null || !location.IsActive)
        {
            return BadRequestError<GymSaleCatalogItemResponse>("invalid_location", "The selected location does not belong to the gym.");
        }

        if (!scope.CanAccessLocation(location.Id))
        {
            return ForbiddenError<GymSaleCatalogItemResponse>("This catalog item belongs to a location outside your staff scope.");
        }

        if (!TryValidateCatalogItemRequest(request, out var errorCode, out var errorMessage))
        {
            return BadRequestError<GymSaleCatalogItemResponse>(errorCode!, errorMessage!);
        }

        item.LocationId = location.Id;
        item.Location = location;
        item.ItemType = request.ItemType;
        item.Name = request.Name.Trim();
        item.UnitPriceAmount = decimal.Round(request.UnitPriceAmount, 2, MidpointRounding.AwayFromZero);
        item.DefaultQuantity = request.DefaultQuantity;
        item.DefaultDiscountAmount = decimal.Round(request.DefaultDiscountAmount, 2, MidpointRounding.AwayFromZero);
        item.DefaultCreditsGranted = request.DefaultCreditsGranted;
        item.ServicePeriodDays = request.ServicePeriodDays;
        item.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        item.IsActive = request.IsActive;
        item.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Success(ResponseMappers.MapSaleCatalogItemResponse(item));
    }

    [HttpPatch("catalog/{catalogItemId:guid}/activation")]
    [Authorize(Policy = AuthorizationPolicies.BillingWrite)]
    public async Task<ActionResult<ApiResponse<GymSaleCatalogItemResponse>>> UpdateCatalogItemActivation(
        Guid gymId,
        Guid catalogItemId,
        [FromBody] UpdateGymSaleCatalogItemActivationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleCatalogItemResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleCatalogItemResponse>();
        }

        var item = await _dbContext.GymSaleCatalogItems
            .Include(entry => entry.Location)
            .SingleOrDefaultAsync(entry => entry.Id == catalogItemId && entry.GymId == gymId, cancellationToken);
        if (item is null)
        {
            return NotFoundError<GymSaleCatalogItemResponse>("Catalog item not found.");
        }

        if (!scope.CanAccessLocation(item.LocationId))
        {
            return ForbiddenError<GymSaleCatalogItemResponse>("This catalog item belongs to a location outside your staff scope.");
        }

        item.IsActive = request.IsActive;
        item.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success(ResponseMappers.MapSaleCatalogItemResponse(item));
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.BillingRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymSaleResponse>>>> GetSales(
        Guid gymId,
        [FromQuery] GymSaleStatus? status,
        [FromQuery] GymSalePaymentStatus? paymentStatus,
        [FromQuery] Guid? membershipId,
        [FromQuery] bool onlyRenewals,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymSaleResponse>>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<GymSaleResponse>>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymSaleResponse>>();
        }

        var salesQuery = _dbContext.GymSales
            .AsNoTracking()
            .Where(sale => sale.GymId == gymId)
            .Include(sale => sale.Location)
            .Include(sale => sale.Membership)
                .ThenInclude(membership => membership.User)
            .Include(sale => sale.Membership)
                .ThenInclude(membership => membership.MemberProfile)
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .OrderByDescending(sale => sale.SoldAtUtc)
            .ThenByDescending(sale => sale.CreatedAtUtc)
            .AsQueryable();

        if (status.HasValue)
        {
            salesQuery = salesQuery.Where(sale => sale.Status == status.Value);
        }

        if (paymentStatus.HasValue)
        {
            salesQuery = salesQuery.Where(sale => sale.PaymentStatus == paymentStatus.Value);
        }

        if (membershipId.HasValue)
        {
            salesQuery = salesQuery.Where(sale => sale.GymMembershipId == membershipId.Value);
        }

        if (fromUtc.HasValue)
        {
            salesQuery = salesQuery.Where(sale => sale.SoldAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            salesQuery = salesQuery.Where(sale => sale.SoldAtUtc <= toUtc.Value);
        }

        if (onlyRenewals)
        {
            salesQuery = salesQuery.Where(sale =>
                sale.Lines.Any(line =>
                    line.ItemType == GymSaleItemType.MembershipPeriodic
                    || line.ItemType == GymSaleItemType.MembershipEntries));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            salesQuery = salesQuery.Where(sale =>
                sale.ReferenceCode.ToLower().Contains(normalizedSearch)
                || (sale.Notes != null && sale.Notes.ToLower().Contains(normalizedSearch))
                || sale.Lines.Any(line => line.Name.ToLower().Contains(normalizedSearch))
                || (sale.Membership.User != null && sale.Membership.User.Email != null && sale.Membership.User.Email.ToLower().Contains(normalizedSearch))
                || (sale.Membership.InvitationEmail != null && sale.Membership.InvitationEmail.ToLower().Contains(normalizedSearch))
                || (sale.Membership.MemberProfile != null
                    && (
                        (sale.Membership.MemberProfile.FirstName != null && sale.Membership.MemberProfile.FirstName.ToLower().Contains(normalizedSearch))
                        || (sale.Membership.MemberProfile.LastName != null && sale.Membership.MemberProfile.LastName.ToLower().Contains(normalizedSearch)))));
        }

        var sales = await salesQuery.ToListAsync(cancellationToken);
        var visibleSales = sales
            .Where(sale => scope.CanAccessLocation(sale.LocationId))
            .Select(ResponseMappers.MapSaleResponse)
            .ToList();

        return Success<IReadOnlyCollection<GymSaleResponse>>(visibleSales);
    }

    [HttpGet("{saleId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.BillingRead)]
    public async Task<ActionResult<ApiResponse<GymSaleResponse>>> GetSale(
        Guid gymId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleResponse>();
        }

        var sale = await LoadSaleAsync(gymId, saleId, cancellationToken);
        if (sale is null)
        {
            return NotFoundError<GymSaleResponse>("Sale not found.");
        }

        if (!scope.CanAccessLocation(sale.LocationId))
        {
            return ForbiddenError<GymSaleResponse>();
        }

        return Success(ResponseMappers.MapSaleResponse(sale));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.BillingWrite)]
    public async Task<ActionResult<ApiResponse<GymSaleResponse>>> CreateSale(
        Guid gymId,
        [FromBody] CreateGymSaleRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleResponse>();
        }

        var gym = await _dbContext.Gyms
            .Include(entity => entity.Locations)
            .SingleOrDefaultAsync(entity => entity.Id == gymId, cancellationToken);

        if (gym is null)
        {
            return NotFoundError<GymSaleResponse>("Gym not found.");
        }

        if (request.Lines.Count == 0)
        {
            return BadRequestError<GymSaleResponse>("missing_lines", "At least one sale line is required.");
        }

        if (request.Payments.Count == 0)
        {
            return BadRequestError<GymSaleResponse>("missing_payments", "At least one payment entry is required.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleResponse>();
        }

        var membership = await _dbContext.GymMemberships
            .Include(entity => entity.User)
            .Include(entity => entity.MemberProfile)
            .Include(entity => entity.Locations)
                .ThenInclude(location => location.Location)
            .SingleOrDefaultAsync(entity => entity.Id == request.MembershipId && entity.GymId == gymId, cancellationToken);

        if (membership is null)
        {
            return NotFoundError<GymSaleResponse>("Membership not found.");
        }

        var locationId = ResolveSaleLocationId(request.LocationId, membership);
        if (locationId == Guid.Empty)
        {
            return ConflictError<GymSaleResponse>("A valid location is required to record a sale.");
        }

        if (!gym.Locations.Any(location => location.Id == locationId && location.IsActive))
        {
            return BadRequestError<GymSaleResponse>("invalid_location", "The selected location does not belong to the gym.");
        }

        if (!membership.Locations.Any(location => location.LocationId == locationId))
        {
            return ConflictError<GymSaleResponse>("The selected member cannot buy on a location outside the membership scope.");
        }

        if (!scope.CanAccessLocation(locationId))
        {
            return ForbiddenError<GymSaleResponse>("This sale belongs to a location outside your staff scope.");
        }

        var now = DateTime.UtcNow;
        var lineEntities = new List<GymSaleLine>(request.Lines.Count);
        decimal subtotalAmount = 0m;
        decimal discountAmount = 0m;

        foreach (var line in request.Lines)
        {
            if (line.ServicePeriodStartUtc.HasValue && line.ServicePeriodEndUtc.HasValue
                && line.ServicePeriodEndUtc.Value < line.ServicePeriodStartUtc.Value)
            {
                return BadRequestError<GymSaleResponse>(
                    "invalid_service_period",
                    $"The service period for line '{line.Name}' is not valid.");
            }

            var quantity = line.Quantity;
            var grossAmount = decimal.Round(quantity * line.UnitPriceAmount, 2, MidpointRounding.AwayFromZero);
            var lineDiscountAmount = decimal.Round(line.DiscountAmount, 2, MidpointRounding.AwayFromZero);
            if (lineDiscountAmount > grossAmount)
            {
                return BadRequestError<GymSaleResponse>(
                    "invalid_discount",
                    $"Discount exceeds gross amount for line '{line.Name}'.");
            }

            var lineTotalAmount = grossAmount - lineDiscountAmount;
            subtotalAmount += grossAmount;
            discountAmount += lineDiscountAmount;

            lineEntities.Add(new GymSaleLine
            {
                Id = Guid.NewGuid(),
                ItemType = line.ItemType,
                Name = line.Name.Trim(),
                Quantity = quantity,
                UnitPriceAmount = decimal.Round(line.UnitPriceAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = lineDiscountAmount,
                LineTotalAmount = decimal.Round(lineTotalAmount, 2, MidpointRounding.AwayFromZero),
                ServicePeriodStartUtc = line.ServicePeriodStartUtc,
                ServicePeriodEndUtc = line.ServicePeriodEndUtc,
                CreditsGranted = line.CreditsGranted,
                Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
            });
        }

        subtotalAmount = decimal.Round(subtotalAmount, 2, MidpointRounding.AwayFromZero);
        discountAmount = decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero);
        var totalAmount = decimal.Round(subtotalAmount - discountAmount, 2, MidpointRounding.AwayFromZero);

        var paymentEntities = new List<GymSalePayment>(request.Payments.Count);
        foreach (var payment in request.Payments)
        {
            if (!TryCreatePaymentEntity(
                    payment.Amount,
                    payment.Method,
                    payment.Status,
                    payment.DueAtUtc,
                    payment.PaidAtUtc,
                    payment.Notes,
                    now,
                    out var paymentEntity,
                    out var errorCode,
                    out var errorMessage))
            {
                return BadRequestError<GymSaleResponse>(errorCode!, errorMessage!);
            }

            paymentEntities.Add(paymentEntity);
        }

        var paidAmount = decimal.Round(
            paymentEntities
                .Where(payment => payment.Status == GymSalePaymentStatus.Paid)
                .Sum(payment => payment.Amount),
            2,
            MidpointRounding.AwayFromZero);

        paidAmount = decimal.Round(paidAmount, 2, MidpointRounding.AwayFromZero);
        if (paidAmount > totalAmount)
        {
            return BadRequestError<GymSaleResponse>("invalid_payment_amount", "Paid amount cannot exceed sale total.");
        }

        var sale = new GymSale
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            GymMembershipId = membership.Id,
            LocationId = locationId,
            ReferenceCode = $"SAL-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20],
            CreatedByUserId = actorUserId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            SoldAtUtc = request.SoldAtUtc ?? now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            SubtotalAmount = subtotalAmount,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            Lines = lineEntities,
            Payments = paymentEntities
        };

        foreach (var payment in sale.Payments)
        {
            SyncPaymentReceiptMetadata(sale.ReferenceCode, payment);
        }

        ApplySalePaymentSummary(sale);
        _dbContext.GymSales.Add(sale);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedSale = await LoadSaleAsync(gymId, sale.Id, cancellationToken);
        return Success(ResponseMappers.MapSaleResponse(savedSale!));
    }

    [HttpPost("{saleId:guid}/payments")]
    [Authorize(Policy = AuthorizationPolicies.BillingWrite)]
    public async Task<ActionResult<ApiResponse<GymSaleResponse>>> AddPayment(
        Guid gymId,
        Guid saleId,
        [FromBody] AddGymSalePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleResponse>();
        }

        var sale = await LoadTrackedSaleAsync(gymId, saleId, cancellationToken);
        if (sale is null)
        {
            return NotFoundError<GymSaleResponse>("Sale not found.");
        }

        if (!scope.CanAccessLocation(sale.LocationId))
        {
            return ForbiddenError<GymSaleResponse>("This sale belongs to a location outside your staff scope.");
        }

        if (sale.Status is GymSaleStatus.Cancelled or GymSaleStatus.Refunded)
        {
            return ConflictError<GymSaleResponse>("Payments cannot be registered on cancelled or refunded sales.");
        }

        if (!TryCreatePaymentEntity(
                request.Amount,
                request.Method,
                request.Status,
                request.DueAtUtc,
                request.PaidAtUtc,
                request.Notes,
                DateTime.UtcNow,
                out var payment,
                out var errorCode,
                out var errorMessage))
        {
            return BadRequestError<GymSaleResponse>(errorCode!, errorMessage!);
        }

        if (payment.Status == GymSalePaymentStatus.Paid
            && decimal.Round(sale.PaidAmount + payment.Amount, 2, MidpointRounding.AwayFromZero) > sale.TotalAmount)
        {
            return BadRequestError<GymSaleResponse>("invalid_payment_amount", "Paid amount cannot exceed sale total.");
        }

        sale.Payments.Add(payment);
        SyncPaymentReceiptMetadata(sale.ReferenceCode, payment);
        sale.UpdatedAtUtc = DateTime.UtcNow;
        ApplySalePaymentSummary(sale);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedSale = await LoadSaleAsync(gymId, sale.Id, cancellationToken);
        return Success(ResponseMappers.MapSaleResponse(savedSale!));
    }

    [HttpPatch("{saleId:guid}/payments/{paymentId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.BillingWrite)]
    public async Task<ActionResult<ApiResponse<GymSaleResponse>>> UpdatePayment(
        Guid gymId,
        Guid saleId,
        Guid paymentId,
        [FromBody] UpdateGymSalePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleResponse>();
        }

        var sale = await LoadTrackedSaleAsync(gymId, saleId, cancellationToken);
        if (sale is null)
        {
            return NotFoundError<GymSaleResponse>("Sale not found.");
        }

        if (!scope.CanAccessLocation(sale.LocationId))
        {
            return ForbiddenError<GymSaleResponse>("This sale belongs to a location outside your staff scope.");
        }

        if (sale.Status is GymSaleStatus.Cancelled or GymSaleStatus.Refunded)
        {
            return ConflictError<GymSaleResponse>("Payments cannot be edited on cancelled or refunded sales.");
        }

        var payment = sale.Payments.SingleOrDefault(item => item.Id == paymentId);
        if (payment is null)
        {
            return NotFoundError<GymSaleResponse>("Payment not found.");
        }

        if (payment.Status == GymSalePaymentStatus.Refunded)
        {
            return ConflictError<GymSaleResponse>("Refund entries are generated automatically and cannot be edited.");
        }

        if (!TryCreatePaymentEntity(
                request.Amount,
                request.Method,
                request.Status,
                request.DueAtUtc,
                request.PaidAtUtc,
                request.Notes,
                DateTime.UtcNow,
                out var candidatePayment,
                out var errorCode,
                out var errorMessage))
        {
            return BadRequestError<GymSaleResponse>(errorCode!, errorMessage!);
        }

        var otherPaidAmount = decimal.Round(
            sale.Payments
                .Where(item => item.Id != paymentId && item.Status == GymSalePaymentStatus.Paid)
                .Sum(item => item.Amount),
            2,
            MidpointRounding.AwayFromZero);

        if (candidatePayment.Status == GymSalePaymentStatus.Paid
            && decimal.Round(otherPaidAmount + candidatePayment.Amount, 2, MidpointRounding.AwayFromZero) > sale.TotalAmount)
        {
            return BadRequestError<GymSaleResponse>("invalid_payment_amount", "Paid amount cannot exceed sale total.");
        }

        payment.Amount = candidatePayment.Amount;
        payment.Method = candidatePayment.Method;
        payment.Status = candidatePayment.Status;
        payment.DueAtUtc = candidatePayment.DueAtUtc;
        payment.PaidAtUtc = candidatePayment.PaidAtUtc;
        payment.Notes = candidatePayment.Notes;
        SyncPaymentReceiptMetadata(sale.ReferenceCode, payment);

        sale.UpdatedAtUtc = DateTime.UtcNow;
        ApplySalePaymentSummary(sale);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedSale = await LoadSaleAsync(gymId, sale.Id, cancellationToken);
        return Success(ResponseMappers.MapSaleResponse(savedSale!));
    }

    [HttpPost("{saleId:guid}/cancel")]
    [Authorize(Policy = AuthorizationPolicies.BillingWrite)]
    public async Task<ActionResult<ApiResponse<GymSaleResponse>>> CancelSale(
        Guid gymId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleResponse>();
        }

        var sale = await LoadTrackedSaleAsync(gymId, saleId, cancellationToken);
        if (sale is null)
        {
            return NotFoundError<GymSaleResponse>("Sale not found.");
        }

        if (!scope.CanAccessLocation(sale.LocationId))
        {
            return ForbiddenError<GymSaleResponse>("This sale belongs to a location outside your staff scope.");
        }

        if (sale.Status == GymSaleStatus.Cancelled)
        {
            return ConflictError<GymSaleResponse>("Sale is already cancelled.");
        }

        if (sale.Status == GymSaleStatus.Refunded)
        {
            return ConflictError<GymSaleResponse>("Refunded sales cannot be cancelled.");
        }

        if (sale.Payments.Any(payment => payment.Status == GymSalePaymentStatus.Paid))
        {
            return ConflictError<GymSaleResponse>("Paid sales cannot be cancelled. Use refund instead.");
        }

        sale.Status = GymSaleStatus.Cancelled;
        sale.PaidAmount = 0m;
        sale.RemainingAmount = 0m;
        sale.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedSale = await LoadSaleAsync(gymId, sale.Id, cancellationToken);
        return Success(ResponseMappers.MapSaleResponse(savedSale!));
    }

    [HttpPost("{saleId:guid}/refund")]
    [Authorize(Policy = AuthorizationPolicies.BillingWrite)]
    public async Task<ActionResult<ApiResponse<GymSaleResponse>>> RefundSale(
        Guid gymId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymSaleResponse>();
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Billing,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymSaleResponse>();
        }

        var sale = await LoadTrackedSaleAsync(gymId, saleId, cancellationToken);
        if (sale is null)
        {
            return NotFoundError<GymSaleResponse>("Sale not found.");
        }

        if (!scope.CanAccessLocation(sale.LocationId))
        {
            return ForbiddenError<GymSaleResponse>("This sale belongs to a location outside your staff scope.");
        }

        if (sale.Status == GymSaleStatus.Cancelled)
        {
            return ConflictError<GymSaleResponse>("Cancelled sales cannot be refunded.");
        }

        if (sale.Status == GymSaleStatus.Refunded)
        {
            return ConflictError<GymSaleResponse>("Sale is already refunded.");
        }

        if (sale.PaymentStatus != GymSalePaymentStatus.Paid || sale.PaidAmount < sale.TotalAmount)
        {
            return ConflictError<GymSaleResponse>("Only fully paid sales can be refunded.");
        }

        var now = DateTime.UtcNow;
        var refundMethod = sale.Payments
            .Where(payment => payment.Status == GymSalePaymentStatus.Paid)
            .OrderByDescending(payment => payment.PaidAtUtc ?? payment.CreatedAtUtc)
            .Select(payment => payment.Method)
            .FirstOrDefault();

        sale.Payments.Add(new GymSalePayment
        {
            Id = Guid.NewGuid(),
            Amount = sale.TotalAmount,
            Method = refundMethod,
            Status = GymSalePaymentStatus.Refunded,
            Notes = "Full sale refund",
            CreatedAtUtc = now
        });

        sale.UpdatedAtUtc = now;
        ApplySalePaymentSummary(sale);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedSale = await LoadSaleAsync(gymId, sale.Id, cancellationToken);
        return Success(ResponseMappers.MapSaleResponse(savedSale!));
    }

    private async Task<GymSale?> LoadSaleAsync(Guid gymId, Guid saleId, CancellationToken cancellationToken)
    {
        return await _dbContext.GymSales
            .AsNoTracking()
            .Include(sale => sale.Location)
            .Include(sale => sale.Membership)
                .ThenInclude(membership => membership.User)
            .Include(sale => sale.Membership)
                .ThenInclude(membership => membership.MemberProfile)
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .SingleOrDefaultAsync(sale => sale.GymId == gymId && sale.Id == saleId, cancellationToken);
    }

    private async Task<GymSale?> LoadTrackedSaleAsync(Guid gymId, Guid saleId, CancellationToken cancellationToken)
    {
        return await _dbContext.GymSales
            .Include(sale => sale.Location)
            .Include(sale => sale.Membership)
                .ThenInclude(membership => membership.User)
            .Include(sale => sale.Membership)
                .ThenInclude(membership => membership.MemberProfile)
            .Include(sale => sale.Lines)
            .Include(sale => sale.Payments)
            .SingleOrDefaultAsync(sale => sale.GymId == gymId && sale.Id == saleId, cancellationToken);
    }

    private static Guid ResolveSaleLocationId(Guid? requestedLocationId, GymMembership membership)
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

    private static GymSalePaymentStatus SummarizePaymentStatus(
        IReadOnlyCollection<GymSalePayment> payments,
        decimal totalAmount,
        decimal paidAmount)
    {
        var refundedAmount = decimal.Round(
            payments
                .Where(payment => payment.Status == GymSalePaymentStatus.Refunded)
                .Sum(payment => payment.Amount),
            2,
            MidpointRounding.AwayFromZero);

        if (refundedAmount >= totalAmount && totalAmount > 0m)
        {
            return GymSalePaymentStatus.Refunded;
        }

        if (paidAmount >= totalAmount && payments.All(payment => payment.Status == GymSalePaymentStatus.Paid))
        {
            return GymSalePaymentStatus.Paid;
        }

        if (paidAmount > 0m)
        {
            return GymSalePaymentStatus.PartiallyPaid;
        }

        if (payments.Any(payment => payment.Status == GymSalePaymentStatus.Failed))
        {
            return GymSalePaymentStatus.Failed;
        }

        return GymSalePaymentStatus.Pending;
    }

    private static bool TryCreatePaymentEntity(
        decimal amount,
        GymSalePaymentMethod method,
        GymSalePaymentStatus status,
        DateTime? dueAtUtc,
        DateTime? paidAtUtc,
        string? notes,
        DateTime now,
        out GymSalePayment payment,
        out string? errorCode,
        out string? errorMessage)
    {
        payment = null!;
        errorCode = null;
        errorMessage = null;

        if (status is GymSalePaymentStatus.PartiallyPaid or GymSalePaymentStatus.Refunded)
        {
            errorCode = "invalid_payment_status";
            errorMessage = "Payment entries can only be pending, paid, or failed.";
            return false;
        }

        var roundedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (roundedAmount <= 0m)
        {
            errorCode = "invalid_payment_amount";
            errorMessage = "Payment amount must be greater than zero.";
            return false;
        }

        if (status != GymSalePaymentStatus.Paid && paidAtUtc.HasValue)
        {
            errorCode = "invalid_paid_at";
            errorMessage = "Paid date can only be provided for paid payments.";
            return false;
        }

        payment = new GymSalePayment
        {
            Id = Guid.NewGuid(),
            Amount = roundedAmount,
            Method = method,
            Status = status,
            DueAtUtc = dueAtUtc,
            PaidAtUtc = status == GymSalePaymentStatus.Paid ? paidAtUtc ?? now : null,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAtUtc = now
        };

        return true;
    }

    private static bool TryValidateCatalogItemRequest(
        UpsertGymSaleCatalogItemRequest request,
        out string? errorCode,
        out string? errorMessage)
    {
        errorCode = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errorCode = "invalid_name";
            errorMessage = "Catalog item name is required.";
            return false;
        }

        if (request.DefaultQuantity <= 0)
        {
            errorCode = "invalid_quantity";
            errorMessage = "Default quantity must be greater than zero.";
            return false;
        }

        var grossAmount = decimal.Round(request.DefaultQuantity * request.UnitPriceAmount, 2, MidpointRounding.AwayFromZero);
        var discountAmount = decimal.Round(request.DefaultDiscountAmount, 2, MidpointRounding.AwayFromZero);
        if (discountAmount > grossAmount)
        {
            errorCode = "invalid_discount";
            errorMessage = "Default discount cannot exceed gross amount.";
            return false;
        }

        if (request.DefaultCreditsGranted.HasValue && request.DefaultCreditsGranted.Value <= 0)
        {
            errorCode = "invalid_credits";
            errorMessage = "Granted credits must be greater than zero.";
            return false;
        }

        if (request.ServicePeriodDays.HasValue && request.ServicePeriodDays.Value <= 0)
        {
            errorCode = "invalid_service_period_days";
            errorMessage = "Service period days must be greater than zero.";
            return false;
        }

        return true;
    }

    private static void SyncPaymentReceiptMetadata(string saleReferenceCode, GymSalePayment payment)
    {
        if (payment.Status != GymSalePaymentStatus.Paid)
        {
            payment.ReceiptCode = null;
            payment.ReceiptIssuedAtUtc = null;
            return;
        }

        payment.ReceiptCode ??= BuildPaymentReceiptCode(saleReferenceCode, payment.Id);
        payment.ReceiptIssuedAtUtc ??= payment.PaidAtUtc ?? payment.CreatedAtUtc;
    }

    private static string BuildPaymentReceiptCode(string saleReferenceCode, Guid paymentId)
    {
        var suffix = paymentId.ToString("N")[..6].ToUpperInvariant();
        return $"RIC-{saleReferenceCode}-{suffix}";
    }

    private static void ApplySalePaymentSummary(GymSale sale)
    {
        var grossPaidAmount = decimal.Round(
            sale.Payments
                .Where(payment => payment.Status == GymSalePaymentStatus.Paid)
                .Sum(payment => payment.Amount),
            2,
            MidpointRounding.AwayFromZero);
        var refundedAmount = decimal.Round(
            sale.Payments
                .Where(payment => payment.Status == GymSalePaymentStatus.Refunded)
                .Sum(payment => payment.Amount),
            2,
            MidpointRounding.AwayFromZero);

        sale.PaidAmount = decimal.Round(Math.Max(0m, grossPaidAmount - refundedAmount), 2, MidpointRounding.AwayFromZero);
        sale.PaymentStatus = SummarizePaymentStatus(sale.Payments.ToList(), sale.TotalAmount, sale.PaidAmount);
        sale.RemainingAmount = sale.PaymentStatus switch
        {
            GymSalePaymentStatus.Refunded => 0m,
            _ => decimal.Round(Math.Max(0m, sale.TotalAmount - sale.PaidAmount), 2, MidpointRounding.AwayFromZero)
        };
        sale.Status = sale.PaymentStatus switch
        {
            GymSalePaymentStatus.Paid => GymSaleStatus.Paid,
            GymSalePaymentStatus.Refunded => GymSaleStatus.Refunded,
            _ => GymSaleStatus.PendingPayment
        };
    }
}
