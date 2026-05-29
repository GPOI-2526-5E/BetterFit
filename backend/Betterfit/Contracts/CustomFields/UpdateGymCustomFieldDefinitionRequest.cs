using System.ComponentModel.DataAnnotations;
using Betterfit.Models;

namespace Betterfit.Contracts.CustomFields;

public sealed class UpdateGymCustomFieldDefinitionRequest
{
    [Required]
    [MaxLength(64)]
    public string Key { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Label { get; init; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; init; }

    public GymCustomFieldValueType ValueType { get; init; } = GymCustomFieldValueType.Text;

    public List<string> Options { get; init; } = [];

    public bool IsRequired { get; init; }

    public bool IsActive { get; init; } = true;

    public int SortOrder { get; init; }
}
