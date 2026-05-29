using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymAccessEvent
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid GymMembershipId { get; set; }

    public Guid LocationId { get; set; }

    [MaxLength(120)]
    public string GateName { get; set; } = string.Empty;

    public GymAccessEventResult Result { get; set; } = GymAccessEventResult.Granted;

    public GymAccessOrigin Origin { get; set; } = GymAccessOrigin.Desk;

    [MaxLength(256)]
    public string? Reason { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? PerformedByUserId { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymMembership Membership { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public ApplicationUser? PerformedByUser { get; set; }
}
