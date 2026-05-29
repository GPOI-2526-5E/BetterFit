namespace Betterfit.Models;

/// <summary>
/// Location scope granted to a tenant membership.
/// </summary>
public class GymMembershipLocation
{
    public Guid Id { get; set; }

    public Guid GymMembershipId { get; set; }

    public Guid LocationId { get; set; }

    public DateTime AssignedAtUtc { get; set; }

    public GymMembership GymMembership { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;
}
