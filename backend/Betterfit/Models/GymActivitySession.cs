using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymActivitySession
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid GymActivityTemplateId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? InstructorAssignmentId { get; set; }

    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(120)]
    public string InstructorName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public DateTime StartsAtUtc { get; set; }

    public DateTime EndsAtUtc { get; set; }

    public GymActivitySessionStatus Status { get; set; } = GymActivitySessionStatus.Scheduled;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymActivityTemplate Template { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public TenantRoleAssignment? InstructorAssignment { get; set; }

    public ICollection<GymActivityBooking> Bookings { get; set; } = new List<GymActivityBooking>();
}
