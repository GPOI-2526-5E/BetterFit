using System.Security.Cryptography;
using System.Globalization;
using System.Text.Json;
using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.Gyms;
using Betterfit.Data;
using Betterfit.Infrastructure.Mapping;
using Betterfit.Infrastructure.Security;
using Betterfit.Models;
using Betterfit.Services.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/memberships")]
public sealed class MembershipsController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAccountProfileService _accountProfileService;

    public MembershipsController(
        AppDbContext dbContext,
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager,
        IAccountProfileService accountProfileService)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
        _userManager = userManager;
        _accountProfileService = accountProfileService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.MembersRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymMembershipResponse>>>> GetGymMemberships(
        Guid gymId,
        CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedError<IReadOnlyCollection<GymMembershipResponse>>();
        }

        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<GymMembershipResponse>>("Gym not found.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            userId,
            gymId,
            PermissionResources.Members,
            PermissionActions.Read,
            cancellationToken);

        if (!scope.HasAnyAccess)
        {
            return ForbiddenError<IReadOnlyCollection<GymMembershipResponse>>();
        }

        var memberships = await LoadMembershipsForGymAsync(gymId, cancellationToken);
        var visibleMemberships = memberships
            .Where(membership => MembershipHasVisibleLocation(membership, scope))
            .Select(membership => ResponseMappers.MapMembershipResponse(membership, scope))
            .ToList();

        return Success<IReadOnlyCollection<GymMembershipResponse>>(visibleMemberships);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MembersWrite)]
    public async Task<ActionResult<ApiResponse<GymMembershipResponse>>> UpsertMembership(
        Guid gymId,
        [FromBody] AssignUserToGymRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymMembershipResponse>();
        }

        var gym = await _dbContext.Gyms
            .Include(x => x.Locations)
            .SingleOrDefaultAsync(x => x.Id == gymId, cancellationToken);

        if (gym is null)
        {
            return NotFoundError<GymMembershipResponse>("Gym not found.");
        }

        var normalizedEmail = LocationHelpers.NormalizeEmail(request.Email);
        var user = await ResolveUserAsync(request.UserId, normalizedEmail);
        if (user is null && string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return BadRequestError<GymMembershipResponse>("invalid_target_user", "Provide a valid userId or email.");
        }

        var membership = await FindMembershipAsync(gymId, user?.Id, normalizedEmail, cancellationToken);
        var now = DateTime.UtcNow;

        if (membership is null)
        {
            membership = new GymMembership
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _dbContext.GymMemberships.Add(membership);
        }

        var requestedStatus = request.Status ?? (user is null
            ? GymMembershipStatus.PendingClaim
            : GymMembershipStatus.Active);

        if (user is null && requestedStatus != GymMembershipStatus.PendingClaim)
        {
            return ConflictError<GymMembershipResponse>("An unclaimed membership must remain in pending-claim status.");
        }

        var requestedLocationIds = ResolveRequestedLocationIds(request.LocationIds, request.PrimaryLocationId, gym.Locations);
        if (requestedLocationIds.Count == 0)
        {
            return ConflictError<GymMembershipResponse>("At least one active location is required for a membership.");
        }

        var validLocationIds = gym.Locations
            .Where(location => location.IsActive && requestedLocationIds.Contains(location.Id))
            .Select(location => location.Id)
            .ToHashSet();

        if (validLocationIds.Count != requestedLocationIds.Count)
        {
            return BadRequestError<GymMembershipResponse>(
                "invalid_location_ids",
                "One or more locations do not belong to the selected gym.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Members,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.CanAccessAllLocations(requestedLocationIds))
        {
            return ForbiddenError<GymMembershipResponse>(
                "This membership change includes locations outside your staff scope.");
        }

        membership.PrimaryLocationId = request.PrimaryLocationId is { } preferredLocationId && validLocationIds.Contains(preferredLocationId)
            ? preferredLocationId
            : requestedLocationIds[0];
        membership.InvitationEmail = normalizedEmail;
        membership.Status = requestedStatus;
        membership.Source = request.Source ?? membership.Source;
        membership.TaxCode = LocationHelpers.NormalizeOptional(request.TaxCode);
        membership.Notes = LocationHelpers.NormalizeOptional(request.Notes);
        membership.UpdatedAtUtc = now;

        if (requestedStatus == GymMembershipStatus.Active)
        {
            membership.JoinedAtUtc ??= now;
            membership.EndedAtUtc = null;
        }
        else if (requestedStatus == GymMembershipStatus.Archived)
        {
            membership.EndedAtUtc ??= now;
        }

        if (user is not null)
        {
            membership.UserId = user.Id;
            membership.ClaimedAtUtc ??= now;
            var memberProfile = await _accountProfileService.EnsureMemberProfileAsync(user, request.Profile, membership, cancellationToken);
            membership.MemberProfileId = memberProfile.Id;
            MembershipHelpers.ClearPendingMemberFields(membership);
        }
        else
        {
            MembershipHelpers.ApplyPendingProfile(membership, request.Profile);
        }

        MembershipHelpers.SyncMembershipLocations(membership, requestedLocationIds, now);
        var customFieldValidationError = await SyncMembershipCustomFieldsAsync(
            gymId,
            membership,
            request.CustomFields,
            now,
            cancellationToken);

        if (customFieldValidationError is not null)
        {
            return BadRequestError<GymMembershipResponse>(
                code: "invalid_custom_fields",
                message: customFieldValidationError);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var savedMembership = await LoadMembershipAsync(membership.Id, cancellationToken);
        return Success(ResponseMappers.MapMembershipResponse(savedMembership!, scope));
    }

    [HttpPost("{membershipId:guid}/invitations")]
    [Authorize(Policy = AuthorizationPolicies.MembersWrite)]
    public async Task<ActionResult<ApiResponse<GymInvitationResponse>>> CreateMembershipInvitation(
        Guid gymId,
        Guid membershipId,
        [FromBody] CreateGymInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return UnauthorizedError<GymInvitationResponse>();
        }

        var membership = await _dbContext.GymMemberships
            .Include(x => x.Locations)
            .SingleOrDefaultAsync(x => x.Id == membershipId && x.GymId == gymId, cancellationToken);

        if (membership is null)
        {
            return NotFoundError<GymInvitationResponse>("Membership not found.");
        }

        if (membership.UserId is not null)
        {
            return ConflictError<GymInvitationResponse>("This membership is already linked to a Betterfit account.");
        }

        if (membership.Status != GymMembershipStatus.PendingClaim)
        {
            return ConflictError<GymInvitationResponse>("Only pending-claim memberships can issue claim invitations.");
        }

        if (string.IsNullOrWhiteSpace(membership.InvitationEmail))
        {
            return ConflictError<GymInvitationResponse>("Membership is missing an invitation email.");
        }

        var scope = await _permissionService.GetGymPermissionScopeAsync(
            actorUserId,
            gymId,
            PermissionResources.Members,
            PermissionActions.Write,
            cancellationToken);

        if (!scope.CanAccessAllLocations(membership.Locations.Select(location => location.LocationId)))
        {
            return ForbiddenError<GymInvitationResponse>(
                "You can only invite memberships that stay within your staff scope.");
        }

        var now = DateTime.UtcNow;
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var invitation = new GymInvitation
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            GymMembershipId = membership.Id,
            Email = membership.InvitationEmail,
            TokenHash = TokenHasher.ComputeTokenHash(token),
            ExpiresAtUtc = now.AddHours(request.ExpiresInHours),
            CreatedAtUtc = now,
            CreatedByUserId = actorUserId
        };

        _dbContext.GymInvitations.Add(invitation);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success(new GymInvitationResponse(
            invitation.Id,
            invitation.GymId,
            invitation.GymMembershipId,
            invitation.Email,
            token,
            invitation.ExpiresAtUtc,
            invitation.CreatedAtUtc));
    }

    private async Task<List<GymMembership>> LoadMembershipsForGymAsync(Guid gymId, CancellationToken cancellationToken)
    {
        return await _dbContext.GymMemberships
            .AsNoTracking()
            .Where(membership => membership.GymId == gymId)
            .Include(membership => membership.Gym)
            .Include(membership => membership.User)
            .Include(membership => membership.MemberProfile)
            .Include(membership => membership.Locations)
                .ThenInclude(location => location.Location)
            .Include(membership => membership.CustomFieldValues)
                .ThenInclude(value => value.FieldDefinition)
            .OrderBy(membership => membership.UserId == null)
            .ThenBy(membership => membership.User != null ? membership.User.Email : membership.InvitationEmail)
            .ToListAsync(cancellationToken);
    }

    private async Task<GymMembership?> LoadMembershipAsync(Guid membershipId, CancellationToken cancellationToken)
    {
        return await _dbContext.GymMemberships
            .AsNoTracking()
            .Include(membership => membership.Gym)
            .Include(membership => membership.User)
            .Include(membership => membership.MemberProfile)
            .Include(membership => membership.Locations)
                .ThenInclude(location => location.Location)
            .Include(membership => membership.CustomFieldValues)
                .ThenInclude(value => value.FieldDefinition)
            .SingleOrDefaultAsync(membership => membership.Id == membershipId, cancellationToken);
    }

    private async Task<GymMembership?> FindMembershipAsync(Guid gymId, string? userId, string? invitationEmail, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var byUser = await _dbContext.GymMemberships
                .Include(membership => membership.Locations)
                .Include(membership => membership.CustomFieldValues)
                .SingleOrDefaultAsync(
                    membership => membership.GymId == gymId && membership.UserId == userId,
                    cancellationToken);

            if (byUser is not null)
            {
                return byUser;
            }
        }

        if (!string.IsNullOrWhiteSpace(invitationEmail))
        {
            return await _dbContext.GymMemberships
                .Include(membership => membership.Locations)
                .Include(membership => membership.CustomFieldValues)
                .SingleOrDefaultAsync(
                    membership => membership.GymId == gymId && membership.InvitationEmail == invitationEmail,
                    cancellationToken);
        }

        return null;
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

    private static List<Guid> ResolveRequestedLocationIds(
        IReadOnlyCollection<Guid> locationIds,
        Guid? primaryLocationId,
        ICollection<GymLocation> gymLocations)
    {
        var resolved = locationIds
            .Where(locationId => locationId != Guid.Empty)
            .Distinct()
            .ToList();

        if (primaryLocationId is { } preferred && preferred != Guid.Empty && !resolved.Contains(preferred))
        {
            resolved.Add(preferred);
        }

        if (resolved.Count > 0)
        {
            return resolved;
        }

        var defaultLocationId = gymLocations
            .Where(location => location.IsActive)
            .OrderBy(location => location.Name)
            .Select(location => location.Id)
            .FirstOrDefault();

        return defaultLocationId == Guid.Empty ? [] : [defaultLocationId];
    }

    private static bool MembershipHasVisibleLocation(GymMembership membership, GymPermissionScope scope)
    {
        return membership.Locations.Any(location => scope.CanAccessLocation(location.LocationId));
    }

    private async Task<string?> SyncMembershipCustomFieldsAsync(
        Guid gymId,
        GymMembership membership,
        IReadOnlyCollection<GymCustomFieldValueInput> inputs,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var definitions = await _dbContext.GymCustomFieldDefinitions
            .Where(field =>
                field.GymId == gymId
                && field.EntityType == GymCustomFieldEntityType.Member
                && field.IsActive)
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Label)
            .ToListAsync(cancellationToken);

        if (definitions.Count == 0)
        {
            return null;
        }

        var inputLookup = inputs
            .Where(input => input.FieldDefinitionId != Guid.Empty)
            .GroupBy(input => input.FieldDefinitionId)
            .ToDictionary(group => group.Key, group => group.Last().Value);

        var definitionIds = definitions.Select(field => field.Id).ToHashSet();
        var unknownIds = inputLookup.Keys.Where(id => !definitionIds.Contains(id)).ToArray();
        if (unknownIds.Length > 0)
        {
            return "Uno o piu campi personalizzati non appartengono al tenant corrente.";
        }

        var existingValues = membership.CustomFieldValues.ToDictionary(value => value.FieldDefinitionId);

        foreach (var definition in definitions)
        {
            inputLookup.TryGetValue(definition.Id, out var rawValue);
            var normalized = NormalizeCustomFieldValue(definition, rawValue, out var validationError);
            if (validationError is not null)
            {
                return $"{definition.Label}: {validationError}";
            }

            if (definition.IsRequired && string.IsNullOrWhiteSpace(normalized))
            {
                return $"Il campo {definition.Label} e obbligatorio.";
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                if (existingValues.TryGetValue(definition.Id, out var existing))
                {
                    _dbContext.GymCustomFieldValues.Remove(existing);
                }

                continue;
            }

            if (existingValues.TryGetValue(definition.Id, out var current))
            {
                current.Value = normalized;
                current.UpdatedAtUtc = now;
                continue;
            }

            membership.CustomFieldValues.Add(new GymCustomFieldValue
            {
                Id = Guid.NewGuid(),
                GymId = gymId,
                FieldDefinitionId = definition.Id,
                GymMembershipId = membership.Id,
                Value = normalized,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        return null;
    }

    private static string? NormalizeCustomFieldValue(
        GymCustomFieldDefinition definition,
        string? rawValue,
        out string? validationError)
    {
        validationError = null;

        var trimmed = string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
        if (trimmed is null)
        {
            return null;
        }

        switch (definition.ValueType)
        {
            case GymCustomFieldValueType.Text:
            case GymCustomFieldValueType.LongText:
                return trimmed;
            case GymCustomFieldValueType.Number:
                if (!decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                {
                    validationError = "inserisci un numero valido.";
                    return null;
                }

                return number.ToString(CultureInfo.InvariantCulture);
            case GymCustomFieldValueType.Date:
                if (!DateOnly.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    validationError = "inserisci una data valida.";
                    return null;
                }

                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            case GymCustomFieldValueType.Boolean:
                if (!bool.TryParse(trimmed, out var boolValue))
                {
                    validationError = "seleziona vero o falso.";
                    return null;
                }

                return boolValue ? "true" : "false";
            case GymCustomFieldValueType.Select:
                var options = ParseOptions(definition.OptionsJson);
                if (!options.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                {
                    validationError = "seleziona un valore previsto dal campo.";
                    return null;
                }

                return options.First(option => string.Equals(option, trimmed, StringComparison.OrdinalIgnoreCase));
            default:
                return trimmed;
        }
    }

    private static IReadOnlyCollection<string> ParseOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(optionsJson) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
