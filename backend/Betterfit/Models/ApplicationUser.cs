using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Betterfit.Models;

/// <summary>
/// Application user account used for authentication in Betterfit.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Optional display name shown in the CRM UI.
    /// </summary>
    [MaxLength(120)]
    public string? FullName { get; set; }

    /// <summary>
    /// Optional member profile for this account.
    /// </summary>
    public MemberProfile? MemberProfile { get; set; }

    /// <summary>
    /// Optional staff profile for this account.
    /// </summary>
    public StaffProfile? StaffProfile { get; set; }

    /// <summary>
    /// Tenant memberships linked to this account.
    /// </summary>
    public ICollection<GymMembership> GymMemberships { get; set; } = new List<GymMembership>();

    /// <summary>
    /// Staff role assignments linked to this account.
    /// </summary>
    public ICollection<TenantRoleAssignment> TenantRoleAssignments { get; set; } = new List<TenantRoleAssignment>();

    /// <summary>
    /// Sales recorded by this staff account.
    /// </summary>
    public ICollection<GymSale> CreatedSales { get; set; } = new List<GymSale>();

    /// <summary>
    /// Access events manually performed by this staff account.
    /// </summary>
    public ICollection<GymAccessEvent> PerformedAccessEvents { get; set; } = new List<GymAccessEvent>();

    /// <summary>
    /// CRM campaigns created by this staff account.
    /// </summary>
    public ICollection<GymCampaign> CreatedCampaigns { get; set; } = new List<GymCampaign>();

    /// <summary>
    /// CRM automation rules created by this staff account.
    /// </summary>
    public ICollection<GymAutomationRule> CreatedAutomationRules { get; set; } = new List<GymAutomationRule>();

    /// <summary>
    /// Workout assignments created by this staff account.
    /// </summary>
    public ICollection<GymWorkoutAssignment> CreatedWorkoutAssignments { get; set; } = new List<GymWorkoutAssignment>();

    /// <summary>
    /// Workout assessments recorded by this staff account.
    /// </summary>
    public ICollection<GymWorkoutAssessment> RecordedWorkoutAssessments { get; set; } = new List<GymWorkoutAssessment>();
}
