using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Gyms;

public sealed class GymCustomFieldValueInput
{
    [Required]
    public Guid FieldDefinitionId { get; init; }

    [MaxLength(1500)]
    public string? Value { get; init; }
}
