using Betterfit.Models;

namespace Betterfit.Contracts.CustomFields;

public sealed record GymCustomFieldDefinitionResponse(
    Guid Id,
    Guid GymId,
    GymCustomFieldEntityType EntityType,
    string Key,
    string Label,
    string? Description,
    GymCustomFieldValueType ValueType,
    IReadOnlyCollection<string> Options,
    bool IsRequired,
    bool IsActive,
    int SortOrder,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
