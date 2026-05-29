using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymCustomFieldValue
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid FieldDefinitionId { get; set; }

    public Guid GymMembershipId { get; set; }

    [MaxLength(1500)]
    public string? Value { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymCustomFieldDefinition FieldDefinition { get; set; } = null!;

    public GymMembership Membership { get; set; } = null!;
}
