using System.ComponentModel.DataAnnotations;

namespace Betterfit.Models;

/// <summary>
/// Represents a tenant organization in Betterfit.
/// </summary>
public class Gym
{
    /// <summary>
    /// Unique identifier of the gym.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Public gym name.
    /// </summary>
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the gym record was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Custom field definitions available in this tenant.
    /// </summary>
    public ICollection<GymCustomFieldDefinition> CustomFieldDefinitions { get; set; } = new List<GymCustomFieldDefinition>();

    /// <summary>
    /// Custom field values captured against tenant entities.
    /// </summary>
    public ICollection<GymCustomFieldValue> CustomFieldValues { get; set; } = new List<GymCustomFieldValue>();

    /// <summary>
    /// User memberships assigned to this gym.
    /// </summary>
    public ICollection<GymMembership> Memberships { get; set; } = new List<GymMembership>();

    /// <summary>
    /// Physical places that belong to this tenant.
    /// </summary>
    public ICollection<GymLocation> Locations { get; set; } = new List<GymLocation>();

    /// <summary>
    /// Role definitions available in this gym.
    /// </summary>
    public ICollection<GymRole> Roles { get; set; } = new List<GymRole>();

    /// <summary>
    /// Authentication requirements applied to accounts that access this tenant.
    /// </summary>
    public GymAuthenticationPolicy? AuthenticationPolicy { get; set; }

    /// <summary>
    /// Staff role assignments active or historical in this gym.
    /// </summary>
    public ICollection<TenantRoleAssignment> StaffAssignments { get; set; } = new List<TenantRoleAssignment>();

    /// <summary>
    /// Claim invitations issued for memberships in this tenant.
    /// </summary>
    public ICollection<GymInvitation> Invitations { get; set; } = new List<GymInvitation>();

    /// <summary>
    /// Sales recorded for this tenant.
    /// </summary>
    public ICollection<GymSale> Sales { get; set; } = new List<GymSale>();

    /// <summary>
    /// Catalog items available to speed up sales registration in this tenant.
    /// </summary>
    public ICollection<GymSaleCatalogItem> SaleCatalogItems { get; set; } = new List<GymSaleCatalogItem>();

    /// <summary>
    /// Access events recorded for this tenant.
    /// </summary>
    public ICollection<GymAccessEvent> AccessEvents { get; set; } = new List<GymAccessEvent>();

    /// <summary>
    /// Activity templates configured for this tenant.
    /// </summary>
    public ICollection<GymActivityTemplate> ActivityTemplates { get; set; } = new List<GymActivityTemplate>();

    /// <summary>
    /// Activity sessions scheduled in this tenant.
    /// </summary>
    public ICollection<GymActivitySession> ActivitySessions { get; set; } = new List<GymActivitySession>();

    /// <summary>
    /// Activity bookings registered in this tenant.
    /// </summary>
    public ICollection<GymActivityBooking> ActivityBookings { get; set; } = new List<GymActivityBooking>();

    /// <summary>
    /// Leads collected by the commercial funnel for this tenant.
    /// </summary>
    public ICollection<GymLead> Leads { get; set; } = new List<GymLead>();

    /// <summary>
    /// Commercial follow-up tasks tracked for tenant leads.
    /// </summary>
    public ICollection<GymLeadTask> LeadTasks { get; set; } = new List<GymLeadTask>();

    /// <summary>
    /// Marketing campaigns configured for this tenant CRM.
    /// </summary>
    public ICollection<GymCampaign> Campaigns { get; set; } = new List<GymCampaign>();

    /// <summary>
    /// Recurring CRM automation rules configured for this tenant.
    /// </summary>
    public ICollection<GymAutomationRule> AutomationRules { get; set; } = new List<GymAutomationRule>();

    /// <summary>
    /// External integrations configured for this tenant.
    /// </summary>
    public ICollection<GymIntegration> Integrations { get; set; } = new List<GymIntegration>();

    /// <summary>
    /// Exercise library configured for this tenant.
    /// </summary>
    public ICollection<GymWorkoutExercise> WorkoutExercises { get; set; } = new List<GymWorkoutExercise>();

    /// <summary>
    /// Workout templates configured for this tenant.
    /// </summary>
    public ICollection<GymWorkoutTemplate> WorkoutTemplates { get; set; } = new List<GymWorkoutTemplate>();

    /// <summary>
    /// Workout assignments created for members in this tenant.
    /// </summary>
    public ICollection<GymWorkoutAssignment> WorkoutAssignments { get; set; } = new List<GymWorkoutAssignment>();

    /// <summary>
    /// Workout assessments recorded for members in this tenant.
    /// </summary>
    public ICollection<GymWorkoutAssessment> WorkoutAssessments { get; set; } = new List<GymWorkoutAssessment>();
}
