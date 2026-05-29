using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymActivityTemplate
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public Guid LocationId { get; set; }

    public Guid? InstructorAssignmentId { get; set; }

    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Category { get; set; } = "Corso";

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(24)]
    public string? ColorHex { get; set; }

    public int Capacity { get; set; } = 20;

    public int DurationMinutes { get; set; } = 60;

    public bool RequiresBooking { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public GymLocation Location { get; set; } = null!;

    public TenantRoleAssignment? InstructorAssignment { get; set; }

    public ICollection<GymActivitySession> Sessions { get; set; } = new List<GymActivitySession>();
}
