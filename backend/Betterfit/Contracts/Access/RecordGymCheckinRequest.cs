using System.ComponentModel.DataAnnotations;

namespace Betterfit.Contracts.Access;

public sealed class RecordGymCheckinRequest
{
    public Guid MembershipId { get; init; }

    public Guid? LocationId { get; init; }

    [MaxLength(120)]
    public string? GateName { get; init; }
}
