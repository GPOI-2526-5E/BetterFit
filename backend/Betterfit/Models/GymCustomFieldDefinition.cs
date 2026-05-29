using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymCustomFieldDefinition
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public GymCustomFieldEntityType EntityType { get; set; }

    [MaxLength(64)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Description { get; set; }

    public GymCustomFieldValueType ValueType { get; set; }

    [MaxLength(1500)]
    public string? OptionsJson { get; set; }

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public ICollection<GymCustomFieldValue> Values { get; set; } = new List<GymCustomFieldValue>();
}
