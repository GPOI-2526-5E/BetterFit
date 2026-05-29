using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Integrations;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/integrations")]
public sealed class IntegrationsController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public IntegrationsController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.IntegrationsRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymIntegrationResponse>>>> GetIntegrations(
        Guid gymId,
        [FromQuery] Guid? locationId,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymIntegrationResponse>>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<GymIntegrationResponse>>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Integrations,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymIntegrationResponse>>();
        }

        var integrations = await _dbContext.GymIntegrations
            .AsNoTracking()
            .Where(integration => integration.GymId == gymId)
            .Include(integration => integration.Location)
            .OrderBy(integration => integration.Type)
            .ToListAsync(cancellationToken);

        var visibleIntegrations = integrations
            .Where(integration => IntegrationIsVisible(integration, scope, locationId))
            .Select(MapIntegrationResponse)
            .ToList();

        return Success<IReadOnlyCollection<GymIntegrationResponse>>(visibleIntegrations);
    }

    [HttpPut("{integrationType}")]
    [Authorize(Policy = AuthorizationPolicies.IntegrationsWrite)]
    public async Task<ActionResult<ApiResponse<GymIntegrationResponse>>> UpsertIntegration(
        Guid gymId,
        string integrationType,
        [FromBody] UpsertGymIntegrationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymIntegrationResponse>();
        }

        if (!TryParseIntegrationType(integrationType, out var parsedType))
        {
            return BadRequestError<GymIntegrationResponse>("invalid_integration_type", "Integration type is not valid.");
        }

        var gym = await _dbContext.Gyms
            .Include(item => item.Locations)
            .SingleOrDefaultAsync(item => item.Id == gymId, cancellationToken);

        if (gym is null)
        {
            return NotFoundError<GymIntegrationResponse>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Integrations,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymIntegrationResponse>();
        }

        if (request.LocationId.HasValue && !gym.Locations.Any(location => location.Id == request.LocationId.Value))
        {
            return BadRequestError<GymIntegrationResponse>("invalid_location_id", "Location does not belong to the target gym.");
        }

        if (!CanWriteIntegration(scope, request.LocationId))
        {
            return ForbiddenError<GymIntegrationResponse>("You do not have permission to manage this integration scope.");
        }

        var now = DateTime.UtcNow;
        var integration = await _dbContext.GymIntegrations
            .Include(item => item.Location)
            .SingleOrDefaultAsync(item => item.GymId == gymId && item.Type == parsedType, cancellationToken);

        if (integration is null)
        {
            integration = new GymIntegration
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                Type = parsedType,
                CreatedAtUtc = now
            };

            _dbContext.GymIntegrations.Add(integration);
        }

        integration.LocationId = request.LocationId;
        integration.DisplayName = request.DisplayName.Trim();
        integration.ProviderName = request.ProviderName.Trim();
        integration.Status = request.Status;
        integration.EndpointUrl = NormalizeOptional(request.EndpointUrl);
        integration.Username = NormalizeOptional(request.Username);
        integration.ExternalAccountId = NormalizeOptional(request.ExternalAccountId);
        integration.SenderIdentity = NormalizeOptional(request.SenderIdentity);
        integration.Notes = NormalizeOptional(request.Notes);
        integration.UpdatedAtUtc = now;

        if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            integration.ApiKey = request.ApiKey.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var saved = await _dbContext.GymIntegrations
            .AsNoTracking()
            .Include(item => item.Location)
            .SingleAsync(item => item.Id == integration.Id, cancellationToken);

        return Success(MapIntegrationResponse(saved));
    }

    [HttpPost("{integrationType}/test")]
    [Authorize(Policy = AuthorizationPolicies.IntegrationsWrite)]
    public async Task<ActionResult<ApiResponse<GymIntegrationResponse>>> TestIntegration(
        Guid gymId,
        string integrationType,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymIntegrationResponse>();
        }

        if (!TryParseIntegrationType(integrationType, out var parsedType))
        {
            return BadRequestError<GymIntegrationResponse>("invalid_integration_type", "Integration type is not valid.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Integrations,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<GymIntegrationResponse>();
        }

        var integration = await _dbContext.GymIntegrations
            .Include(item => item.Location)
            .SingleOrDefaultAsync(item => item.GymId == gymId && item.Type == parsedType, cancellationToken);

        if (integration is null)
        {
            return NotFoundError<GymIntegrationResponse>("Integration not configured.");
        }

        if (!CanWriteIntegration(scope, integration.LocationId))
        {
            return ForbiddenError<GymIntegrationResponse>("You do not have permission to manage this integration scope.");
        }

        var validationErrors = ValidateIntegrationConfiguration(integration);
        integration.LastSyncAttemptAtUtc = DateTime.UtcNow;
        integration.LastSyncSucceeded = validationErrors.Count == 0;
        integration.LastSyncMessage = validationErrors.Count == 0
            ? "Configurazione valida. Integrazione pronta per attivazione operativa."
            : $"Campi mancanti o incompleti: {string.Join(", ", validationErrors)}";
        integration.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success(MapIntegrationResponse(integration));
    }

    private static bool TryParseIntegrationType(string value, out GymIntegrationType parsedType)
    {
        return Enum.TryParse(value, ignoreCase: true, out parsedType);
    }

    private static bool IntegrationIsVisible(
        GymIntegration integration,
        GymPermissionScope scope,
        Guid? requestedLocationId)
    {
        if (requestedLocationId.HasValue)
        {
            return integration.LocationId == requestedLocationId.Value
                || integration.LocationId is null && scope.HasTenantWideAccess;
        }

        return integration.LocationId.HasValue
            ? scope.CanAccessLocation(integration.LocationId.Value)
            : scope.HasTenantWideAccess;
    }

    private static bool CanWriteIntegration(GymPermissionScope scope, Guid? locationId)
    {
        return locationId.HasValue
            ? scope.CanAccessLocation(locationId.Value)
            : scope.HasTenantWideAccess;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static GymIntegrationResponse MapIntegrationResponse(GymIntegration integration)
    {
        return new GymIntegrationResponse(
            integration.Id,
            integration.GymId,
            integration.LocationId,
            integration.Location?.Name,
            integration.Type,
            integration.DisplayName,
            integration.ProviderName,
            integration.Status,
            integration.EndpointUrl,
            integration.Username,
            integration.ExternalAccountId,
            integration.SenderIdentity,
            integration.Notes,
            !string.IsNullOrWhiteSpace(integration.ApiKey),
            integration.LastSyncAttemptAtUtc,
            integration.LastSyncSucceeded,
            integration.LastSyncMessage,
            integration.CreatedAtUtc,
            integration.UpdatedAtUtc);
    }

    private static IReadOnlyCollection<string> ValidateIntegrationConfiguration(GymIntegration integration)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(integration.ProviderName))
        {
            missingFields.Add("provider");
        }

        if (string.IsNullOrWhiteSpace(integration.EndpointUrl))
        {
            missingFields.Add("endpoint");
        }

        if (string.IsNullOrWhiteSpace(integration.ApiKey))
        {
            missingFields.Add("credential");
        }

        switch (integration.Type)
        {
            case GymIntegrationType.EmailDelivery:
                if (string.IsNullOrWhiteSpace(integration.Username))
                {
                    missingFields.Add("username");
                }

                if (string.IsNullOrWhiteSpace(integration.SenderIdentity))
                {
                    missingFields.Add("sender identity");
                }
                break;

            case GymIntegrationType.WhatsAppMessaging:
                if (string.IsNullOrWhiteSpace(integration.SenderIdentity))
                {
                    missingFields.Add("sender identity");
                }
                break;

            case GymIntegrationType.AccessControl:
                if (!integration.LocationId.HasValue)
                {
                    missingFields.Add("location");
                }
                break;

            case GymIntegrationType.AccountingExport:
                if (string.IsNullOrWhiteSpace(integration.ExternalAccountId))
                {
                    missingFields.Add("external account");
                }
                break;
        }

        return missingFields;
    }
}
