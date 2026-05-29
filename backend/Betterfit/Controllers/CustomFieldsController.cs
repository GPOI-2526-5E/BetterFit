using System.Text.Json;
using Betterfit.Authorization;
using Betterfit.Contracts.Common;
using Betterfit.Contracts.CustomFields;
using Betterfit.Data;
using Betterfit.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Controllers;

[ApiController]
[Authorize]
[Route("api/gyms/{gymId:guid}/custom-fields")]
public sealed class CustomFieldsController : ApiControllerBase
{
    private readonly AppDbContext _dbContext;

    public CustomFieldsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.MembersRead)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<GymCustomFieldDefinitionResponse>>>> GetDefinitions(
        Guid gymId,
        [FromQuery] GymCustomFieldEntityType? entityType,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<IReadOnlyCollection<GymCustomFieldDefinitionResponse>>("Gym not found.");
        }

        var query = _dbContext.GymCustomFieldDefinitions
            .AsNoTracking()
            .Where(field => field.GymId == gymId);

        if (entityType.HasValue)
        {
            query = query.Where(field => field.EntityType == entityType.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(field => field.IsActive == isActive.Value);
        }

        var definitions = await query
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Label)
            .ToListAsync(cancellationToken);

        return Success<IReadOnlyCollection<GymCustomFieldDefinitionResponse>>(
            definitions.Select(MapDefinitionResponse).ToList());
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.MembersWrite)]
    public async Task<ActionResult<ApiResponse<GymCustomFieldDefinitionResponse>>> CreateDefinition(
        Guid gymId,
        [FromBody] CreateGymCustomFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.Gyms.AnyAsync(gym => gym.Id == gymId, cancellationToken))
        {
            return NotFoundError<GymCustomFieldDefinitionResponse>("Gym not found.");
        }

        var normalizedKey = NormalizeFieldKey(request.Key);
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return BadRequestError<GymCustomFieldDefinitionResponse>("invalid_custom_field_key", "Field key is required.");
        }

        var normalizedLabel = NormalizeFieldLabel(request.Label);
        if (string.IsNullOrWhiteSpace(normalizedLabel))
        {
            return BadRequestError<GymCustomFieldDefinitionResponse>("invalid_custom_field_label", "Field label is required.");
        }

        var options = NormalizeOptions(request.Options);
        var optionsValidationError = ValidateOptions(request.ValueType, options);
        if (optionsValidationError is not null)
        {
            return BadRequestError<GymCustomFieldDefinitionResponse>("invalid_custom_field_options", optionsValidationError);
        }

        var fieldExists = await _dbContext.GymCustomFieldDefinitions.AnyAsync(
            field => field.GymId == gymId
                && field.EntityType == request.EntityType
                && field.Key == normalizedKey,
            cancellationToken);

        if (fieldExists)
        {
            return ConflictError<GymCustomFieldDefinitionResponse>("A custom field with this key already exists for the selected entity.");
        }

        var now = DateTime.UtcNow;
        var definition = new GymCustomFieldDefinition
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            EntityType = request.EntityType,
            Key = normalizedKey,
            Label = normalizedLabel,
            Description = NormalizeOptional(request.Description),
            ValueType = request.ValueType,
            OptionsJson = SerializeOptions(options),
            IsRequired = request.IsRequired,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.GymCustomFieldDefinitions.Add(definition);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success(MapDefinitionResponse(definition));
    }

    [HttpPut("{fieldDefinitionId:guid}")]
    [Authorize(Policy = AuthorizationPolicies.MembersWrite)]
    public async Task<ActionResult<ApiResponse<GymCustomFieldDefinitionResponse>>> UpdateDefinition(
        Guid gymId,
        Guid fieldDefinitionId,
        [FromBody] UpdateGymCustomFieldDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var definition = await _dbContext.GymCustomFieldDefinitions
            .SingleOrDefaultAsync(field => field.GymId == gymId && field.Id == fieldDefinitionId, cancellationToken);

        if (definition is null)
        {
            return NotFoundError<GymCustomFieldDefinitionResponse>("Custom field not found.");
        }

        var normalizedKey = NormalizeFieldKey(request.Key);
        if (string.IsNullOrWhiteSpace(normalizedKey))
        {
            return BadRequestError<GymCustomFieldDefinitionResponse>("invalid_custom_field_key", "Field key is required.");
        }

        var normalizedLabel = NormalizeFieldLabel(request.Label);
        if (string.IsNullOrWhiteSpace(normalizedLabel))
        {
            return BadRequestError<GymCustomFieldDefinitionResponse>("invalid_custom_field_label", "Field label is required.");
        }

        var options = NormalizeOptions(request.Options);
        var optionsValidationError = ValidateOptions(request.ValueType, options);
        if (optionsValidationError is not null)
        {
            return BadRequestError<GymCustomFieldDefinitionResponse>("invalid_custom_field_options", optionsValidationError);
        }

        var fieldExists = await _dbContext.GymCustomFieldDefinitions.AnyAsync(
            field => field.GymId == gymId
                && field.EntityType == definition.EntityType
                && field.Id != fieldDefinitionId
                && field.Key == normalizedKey,
            cancellationToken);

        if (fieldExists)
        {
            return ConflictError<GymCustomFieldDefinitionResponse>("A custom field with this key already exists for the selected entity.");
        }

        definition.Key = normalizedKey;
        definition.Label = normalizedLabel;
        definition.Description = NormalizeOptional(request.Description);
        definition.ValueType = request.ValueType;
        definition.OptionsJson = SerializeOptions(options);
        definition.IsRequired = request.IsRequired;
        definition.IsActive = request.IsActive;
        definition.SortOrder = request.SortOrder;
        definition.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Success(MapDefinitionResponse(definition));
    }

    private static GymCustomFieldDefinitionResponse MapDefinitionResponse(GymCustomFieldDefinition definition)
    {
        return new GymCustomFieldDefinitionResponse(
            definition.Id,
            definition.GymId,
            definition.EntityType,
            definition.Key,
            definition.Label,
            definition.Description,
            definition.ValueType,
            ParseOptions(definition.OptionsJson),
            definition.IsRequired,
            definition.IsActive,
            definition.SortOrder,
            definition.CreatedAtUtc,
            definition.UpdatedAtUtc);
    }

    private static string NormalizeFieldKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return string.Join(
            "_",
            value
                .Trim()
                .ToLowerInvariant()
                .Split([' ', '-', '.'], StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeFieldLabel(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static List<string> NormalizeOptions(IEnumerable<string>? values)
    {
        return (values ?? [])
            .Select(option => option.Trim())
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ValidateOptions(GymCustomFieldValueType valueType, IReadOnlyCollection<string> options)
    {
        if (valueType == GymCustomFieldValueType.Select && options.Count == 0)
        {
            return "Select fields require at least one option.";
        }

        if (valueType != GymCustomFieldValueType.Select && options.Count > 0)
        {
            return "Only select fields can define options.";
        }

        return null;
    }

    private static string? SerializeOptions(IReadOnlyCollection<string> options)
    {
        return options.Count == 0 ? null : JsonSerializer.Serialize(options);
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
