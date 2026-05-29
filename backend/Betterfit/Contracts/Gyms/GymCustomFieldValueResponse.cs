using Betterfit.Models;

namespace Betterfit.Contracts.Gyms;

public sealed record GymCustomFieldValueResponse(
    Guid FieldDefinitionId,
    string Key,
    string Label,
    string? Description,
    GymCustomFieldValueType ValueType,
    IReadOnlyCollection<string> Options,
    bool IsRequired,
    bool IsActive,
    int SortOrder,
    string? Value);
