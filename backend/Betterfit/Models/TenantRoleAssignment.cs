namespace Betterfit.Models;

/// <summary>
/// Staff privilege assignment within a tenant or specific physical location.
/// </summary>
public class TenantRoleAssignment
{
    public Guid Id { get; set; }

    public Guid GymId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public Guid StaffProfileId { get; set; }

    public Guid RoleId { get; set; }

    public TenantRoleAssignmentScopeType ScopeType { get; set; } = TenantRoleAssignmentScopeType.Tenant;

    public Guid? ScopeLocationId { get; set; }

    public bool IsPrimaryOwner { get; set; }

    public TenantRoleAssignmentStatus Status { get; set; } = TenantRoleAssignmentStatus.Active;

    public DateTime GrantedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public Gym Gym { get; set; } = null!;

    public ApplicationUser User { get; set; } = null!;

    public StaffProfile StaffProfile { get; set; } = null!;

    public GymRole Role { get; set; } = null!;

    public GymLocation? ScopeLocation { get; set; }

    public ICollection<GymActivityTemplate> ActivityTemplates { get; set; } = new List<GymActivityTemplate>();

    public ICollection<GymActivitySession> ActivitySessions { get; set; } = new List<GymActivitySession>();

    public ICollection<GymLead> OwnedLeads { get; set; } = new List<GymLead>();

    public ICollection<GymLeadTask> LeadTasks { get; set; } = new List<GymLeadTask>();

    public ICollection<GymCampaign> Campaigns { get; set; } = new List<GymCampaign>();

    public ICollection<GymAutomationRule> AutomationRules { get; set; } = new List<GymAutomationRule>();

    public ICollection<GymWorkoutTemplate> WorkoutTemplates { get; set; } = new List<GymWorkoutTemplate>();

    public ICollection<GymWorkoutAssignment> WorkoutAssignments { get; set; } = new List<GymWorkoutAssignment>();

    public ICollection<GymWorkoutAssessment> WorkoutAssessments { get; set; } = new List<GymWorkoutAssessment>();
}
