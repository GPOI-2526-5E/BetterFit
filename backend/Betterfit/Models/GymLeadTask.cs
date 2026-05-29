using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

public class GymLeadTask
{
    public Guid Id { get; set; }

    public Guid GymLeadId { get; set; }

    public Guid GymId { get; set; }

    public Guid? AssignedAssignmentId { get; set; }

    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public GymLeadTaskStatus Status { get; set; } = GymLeadTaskStatus.Open;

    public DateTime? DueAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public GymLead Lead { get; set; } = null!;

    public Gym Gym { get; set; } = null!;

    public TenantRoleAssignment? AssignedAssignment { get; set; }
}
