using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymActivityBooking
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid GymActivitySessionId { get; set; }

    public Guid GymMembershipId { get; set; }

    public GymActivityBookingStatus Status { get; set; } = GymActivityBookingStatus.Booked;

    public DateTime BookedAtUtc { get; set; }

    public DateTime? CheckedInAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymActivitySession Session { get; set; } = null!;

    public GymMembership Membership { get; set; } = null!;
}
