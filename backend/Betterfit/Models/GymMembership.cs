namespace Betterfit.Models;

/// <summary>
/// Tenant-scoped relationship between a Betterfit account and a gym.
/// </summary>
public class GymMembership
{
    /// <summary>
    /// Unique identifier of the membership assignment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Identifier of the assigned user. Null until the invited user claims the membership.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Global member profile linked to the account. Null until the account is linked.
    /// </summary>
    public Guid? MemberProfileId { get; set; }

    /// <summary>
    /// Identifier of the gym where the user is assigned.
    /// </summary>
    public Guid GymId { get; set; }

    /// <summary>
    /// Email address used for invitation-based onboarding before the account is linked.
    /// </summary>
    public string? InvitationEmail { get; set; }

    /// <summary>
    /// High-level lifecycle state for the membership.
    /// </summary>
    public GymMembershipStatus Status { get; set; } = GymMembershipStatus.PendingClaim;

    /// <summary>
    /// Primary location used as the default place for staff and member context.
    /// </summary>
    public Guid? PrimaryLocationId { get; set; }

    /// <summary>
    /// High-level origin of the membership onboarding.
    /// </summary>
    public GymMembershipSource Source { get; set; } = GymMembershipSource.StaffInvite;

    /// <summary>
    /// Tenant-scoped legal or fiscal code when required.
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Tenant-scoped notes for reception or administration.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// UTC timestamp when the member joined the tenant.
    /// </summary>
    public DateTime? JoinedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the tenant relationship ended.
    /// </summary>
    public DateTime? EndedAtUtc { get; set; }

    /// <summary>
    /// Pending first name collected before the account is claimed.
    /// </summary>
    public string? PendingFirstName { get; set; }

    /// <summary>
    /// Pending last name collected before the account is claimed.
    /// </summary>
    public string? PendingLastName { get; set; }

    /// <summary>
    /// Pending phone number collected before the account is claimed.
    /// </summary>
    public string? PendingPhoneNumber { get; set; }

    /// <summary>
    /// Pending date of birth collected before the account is claimed.
    /// </summary>
    public DateOnly? PendingDateOfBirth { get; set; }

    /// <summary>
    /// Pending emergency contact name collected before the account is claimed.
    /// </summary>
    public string? PendingEmergencyContactName { get; set; }

    /// <summary>
    /// Pending emergency contact phone number collected before the account is claimed.
    /// </summary>
    public string? PendingEmergencyContactPhoneNumber { get; set; }

    /// <summary>
    /// UTC timestamp when the membership was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the membership was last changed.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the invited membership was claimed by a Betterfit account.
    /// </summary>
    public DateTime? ClaimedAtUtc { get; set; }

    /// <summary>
    /// Navigation property to the assigned user.
    /// </summary>
    public ApplicationUser? User { get; set; }

    /// <summary>
    /// Navigation property to the account member profile.
    /// </summary>
    public MemberProfile? MemberProfile { get; set; }

    /// <summary>
    /// Navigation property to the assigned gym.
    /// </summary>
    public Gym Gym { get; set; } = null!;

    /// <summary>
    /// Default location associated with this membership.
    /// </summary>
    public GymLocation? PrimaryLocation { get; set; }

    /// <summary>
    /// Physical places the membership is allowed to operate in.
    /// </summary>
    public ICollection<GymMembershipLocation> Locations { get; set; } = new List<GymMembershipLocation>();

    /// <summary>
    /// Tenant-scoped custom field values associated with this membership.
    /// </summary>
    public ICollection<GymCustomFieldValue> CustomFieldValues { get; set; } = new List<GymCustomFieldValue>();

    /// <summary>
    /// Invitations generated for this membership.
    /// </summary>
    public ICollection<GymInvitation> Invitations { get; set; } = new List<GymInvitation>();

    /// <summary>
    /// Sales recorded against this member inside the tenant.
    /// </summary>
    public ICollection<GymSale> Sales { get; set; } = new List<GymSale>();

    /// <summary>
    /// Access events recorded for this membership.
    /// </summary>
    public ICollection<GymAccessEvent> AccessEvents { get; set; } = new List<GymAccessEvent>();

    /// <summary>
    /// Activity bookings registered for this membership.
    /// </summary>
    public ICollection<GymActivityBooking> ActivityBookings { get; set; } = new List<GymActivityBooking>();

    /// <summary>
    /// Leads converted into this membership.
    /// </summary>
    public ICollection<GymLead> ConvertedLeads { get; set; } = new List<GymLead>();

    /// <summary>
    /// Workout plans assigned to this membership.
    /// </summary>
    public ICollection<GymWorkoutAssignment> WorkoutAssignments { get; set; } = new List<GymWorkoutAssignment>();

    /// <summary>
    /// Workout assessments recorded for this membership.
    /// </summary>
    public ICollection<GymWorkoutAssessment> WorkoutAssessments { get; set; } = new List<GymWorkoutAssessment>();
}
