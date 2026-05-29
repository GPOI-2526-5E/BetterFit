using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

/// <summary>
/// Physical place that belongs to a tenant gym.
/// </summary>
public class GymLocation
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? Code { get; set; }

    [MaxLength(200)]
    public string? AddressLine1 { get; set; }

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? CountryCode { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public ICollection<GymMembershipLocation> Memberships { get; set; } = new List<GymMembershipLocation>();

    public ICollection<GymMembership> PrimaryMemberships { get; set; } = new List<GymMembership>();

    public ICollection<TenantRoleAssignment> StaffAssignments { get; set; } = new List<TenantRoleAssignment>();

    public ICollection<GymSale> Sales { get; set; } = new List<GymSale>();

    public ICollection<GymSaleCatalogItem> SaleCatalogItems { get; set; } = new List<GymSaleCatalogItem>();

    public ICollection<GymAccessEvent> AccessEvents { get; set; } = new List<GymAccessEvent>();

    public ICollection<GymActivityTemplate> ActivityTemplates { get; set; } = new List<GymActivityTemplate>();

    public ICollection<GymActivitySession> ActivitySessions { get; set; } = new List<GymActivitySession>();

    public ICollection<GymLead> Leads { get; set; } = new List<GymLead>();

    public ICollection<GymCampaign> Campaigns { get; set; } = new List<GymCampaign>();

    public ICollection<GymAutomationRule> AutomationRules { get; set; } = new List<GymAutomationRule>();

    public ICollection<GymIntegration> Integrations { get; set; } = new List<GymIntegration>();

    public ICollection<GymWorkoutTemplate> WorkoutTemplates { get; set; } = new List<GymWorkoutTemplate>();

    public ICollection<GymWorkoutAssignment> WorkoutAssignments { get; set; } = new List<GymWorkoutAssignment>();

    public ICollection<GymWorkoutAssessment> WorkoutAssessments { get; set; } = new List<GymWorkoutAssessment>();
}
