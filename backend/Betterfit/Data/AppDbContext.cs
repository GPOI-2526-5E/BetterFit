using Betterfit.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Gym> Gyms => Set<Gym>();
    public DbSet<GymLocation> GymLocations => Set<GymLocation>();
    public DbSet<GymRole> GymRoles => Set<GymRole>();
    public DbSet<PermissionCatalogItem> PermissionCatalogItems => Set<PermissionCatalogItem>();
    public DbSet<GymCustomFieldDefinition> GymCustomFieldDefinitions => Set<GymCustomFieldDefinition>();
    public DbSet<GymCustomFieldValue> GymCustomFieldValues => Set<GymCustomFieldValue>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<GymMembership> GymMemberships => Set<GymMembership>();
    public DbSet<GymMembershipLocation> GymMembershipLocations => Set<GymMembershipLocation>();
    public DbSet<GymAccessEvent> GymAccessEvents => Set<GymAccessEvent>();
    public DbSet<GymActivityTemplate> GymActivityTemplates => Set<GymActivityTemplate>();
    public DbSet<GymActivitySession> GymActivitySessions => Set<GymActivitySession>();
    public DbSet<GymActivityBooking> GymActivityBookings => Set<GymActivityBooking>();
    public DbSet<GymLead> GymLeads => Set<GymLead>();
    public DbSet<GymLeadTask> GymLeadTasks => Set<GymLeadTask>();
    public DbSet<GymCampaign> GymCampaigns => Set<GymCampaign>();
    public DbSet<GymAutomationRule> GymAutomationRules => Set<GymAutomationRule>();
    public DbSet<GymIntegration> GymIntegrations => Set<GymIntegration>();
    public DbSet<GymWorkoutExercise> GymWorkoutExercises => Set<GymWorkoutExercise>();
    public DbSet<GymWorkoutTemplate> GymWorkoutTemplates => Set<GymWorkoutTemplate>();
    public DbSet<GymWorkoutTemplateItem> GymWorkoutTemplateItems => Set<GymWorkoutTemplateItem>();
    public DbSet<GymWorkoutAssignment> GymWorkoutAssignments => Set<GymWorkoutAssignment>();
    public DbSet<GymWorkoutAssignmentItem> GymWorkoutAssignmentItems => Set<GymWorkoutAssignmentItem>();
    public DbSet<GymWorkoutAssessment> GymWorkoutAssessments => Set<GymWorkoutAssessment>();
    public DbSet<GymSale> GymSales => Set<GymSale>();
    public DbSet<GymSaleCatalogItem> GymSaleCatalogItems => Set<GymSaleCatalogItem>();
    public DbSet<GymSaleLine> GymSaleLines => Set<GymSaleLine>();
    public DbSet<GymSalePayment> GymSalePayments => Set<GymSalePayment>();
    public DbSet<TenantRoleAssignment> TenantRoleAssignments => Set<TenantRoleAssignment>();
    public DbSet<GymInvitation> GymInvitations => Set<GymInvitation>();
    public DbSet<GymAuthenticationPolicy> GymAuthenticationPolicies => Set<GymAuthenticationPolicy>();
    public DbSet<AuthenticationChallenge> AuthenticationChallenges => Set<AuthenticationChallenge>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Gym>(entity =>
        {
            entity.ToTable("Gyms");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
        });

        builder.Entity<GymAuthenticationPolicy>(entity =>
        {
            entity.ToTable("GymAuthenticationPolicies");
            entity.HasKey(x => x.GymId);
            entity.Property(x => x.RequireTwoFactorForStaff).IsRequired();
            entity.Property(x => x.RequireTwoFactorForMembers).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity
                .HasOne(x => x.Gym)
                .WithOne(x => x.AuthenticationPolicy)
                .HasForeignKey<GymAuthenticationPolicy>(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymLocation>(entity =>
        {
            entity.ToTable("GymLocations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(64);
            entity.Property(x => x.AddressLine1).HasMaxLength(200);
            entity.Property(x => x.City).HasMaxLength(120);
            entity.Property(x => x.CountryCode).HasMaxLength(2);
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.GymId, x.Code }).IsUnique();

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Locations)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymRole>(entity =>
        {
            entity.ToTable("GymRoles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.NormalizedName }).IsUnique();

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymIntegration>(entity =>
        {
            entity.ToTable("GymIntegrations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).HasConversion<string>().IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.ProviderName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.EndpointUrl).HasMaxLength(512);
            entity.Property(x => x.Username).HasMaxLength(160);
            entity.Property(x => x.ApiKey).HasMaxLength(256);
            entity.Property(x => x.ExternalAccountId).HasMaxLength(160);
            entity.Property(x => x.SenderIdentity).HasMaxLength(160);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.LastSyncMessage).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.Type }).IsUnique();
            entity.HasIndex(x => new { x.GymId, x.LocationId });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Integrations)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.Integrations)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PermissionCatalogItem>(entity =>
        {
            entity.ToTable("PermissionCatalogItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Resource).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedResource).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedAction).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DescriptionKey).HasMaxLength(500);

            entity.HasIndex(x => new { x.NormalizedResource, x.NormalizedAction }).IsUnique();
        });

        builder.Entity<GymCustomFieldDefinition>(entity =>
        {
            entity.ToTable("GymCustomFieldDefinitions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntityType).HasConversion<string>().IsRequired();
            entity.Property(x => x.Key).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Label).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.Property(x => x.ValueType).HasConversion<string>().IsRequired();
            entity.Property(x => x.OptionsJson).HasMaxLength(1500);
            entity.Property(x => x.IsRequired).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.SortOrder).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.EntityType, x.Key }).IsUnique();
            entity.HasIndex(x => new { x.GymId, x.EntityType, x.IsActive, x.SortOrder });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.CustomFieldDefinitions)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymCustomFieldValue>(entity =>
        {
            entity.ToTable("GymCustomFieldValues");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Value).HasMaxLength(1500);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymMembershipId, x.FieldDefinitionId }).IsUnique();
            entity.HasIndex(x => new { x.GymId, x.FieldDefinitionId });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.CustomFieldValues)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.FieldDefinition)
                .WithMany(x => x.Values)
                .HasForeignKey(x => x.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Membership)
                .WithMany(x => x.CustomFieldValues)
                .HasForeignKey(x => x.GymMembershipId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermissions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.IsAllowed).IsRequired();
            entity.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();

            entity
                .HasOne(x => x.Role)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MemberProfile>(entity =>
        {
            entity.ToTable("MemberProfiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.AvatarUrl).HasMaxLength(256);
            entity.Property(x => x.EmergencyContactName).HasMaxLength(100);
            entity.Property(x => x.EmergencyContactPhoneNumber).HasMaxLength(32);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();

            entity
                .HasOne(x => x.User)
                .WithOne(x => x.MemberProfile)
                .HasForeignKey<MemberProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<StaffProfile>(entity =>
        {
            entity.ToTable("StaffProfiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(120);
            entity.Property(x => x.JobTitle).HasMaxLength(120);
            entity.Property(x => x.InternalCode).HasMaxLength(64);
            entity.Property(x => x.Active).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();

            entity
                .HasOne(x => x.User)
                .WithOne(x => x.StaffProfile)
                .HasForeignKey<StaffProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymMembership>(entity =>
        {
            entity.ToTable("GymMemberships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InvitationEmail).HasMaxLength(256);
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.Source).HasConversion<string>().IsRequired();
            entity.Property(x => x.TaxCode).HasMaxLength(32);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.PendingFirstName).HasMaxLength(100);
            entity.Property(x => x.PendingLastName).HasMaxLength(100);
            entity.Property(x => x.PendingPhoneNumber).HasMaxLength(32);
            entity.Property(x => x.PendingEmergencyContactName).HasMaxLength(100);
            entity.Property(x => x.PendingEmergencyContactPhoneNumber).HasMaxLength(32);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.GymId, x.InvitationEmail }).IsUnique();

            entity
                .HasOne(x => x.User)
                .WithMany(x => x.GymMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.MemberProfile)
                .WithMany(x => x.GymMemberships)
                .HasForeignKey(x => x.MemberProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.PrimaryLocation)
                .WithMany(x => x.PrimaryMemberships)
                .HasForeignKey(x => x.PrimaryLocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymMembershipLocation>(entity =>
        {
            entity.ToTable("GymMembershipLocations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AssignedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.GymMembershipId, x.LocationId }).IsUnique();

            entity
                .HasOne(x => x.GymMembership)
                .WithMany(x => x.Locations)
                .HasForeignKey(x => x.GymMembershipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymAccessEvent>(entity =>
        {
            entity.ToTable("GymAccessEvents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.GateName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Result).HasConversion<string>().IsRequired();
            entity.Property(x => x.Origin).HasConversion<string>().IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(256);
            entity.Property(x => x.OccurredAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.GymMembershipId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.LocationId, x.OccurredAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.AccessEvents)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Membership)
                .WithMany(x => x.AccessEvents)
                .HasForeignKey(x => x.GymMembershipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.AccessEvents)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.PerformedByUser)
                .WithMany(x => x.PerformedAccessEvents)
                .HasForeignKey(x => x.PerformedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymActivityTemplate>(entity =>
        {
            entity.ToTable("GymActivityTemplates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.ColorHex).HasMaxLength(24);
            entity.Property(x => x.Capacity).IsRequired();
            entity.Property(x => x.DurationMinutes).IsRequired();
            entity.Property(x => x.RequiresBooking).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.Name, x.LocationId }).IsUnique();

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.ActivityTemplates)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.ActivityTemplates)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.InstructorAssignment)
                .WithMany(x => x.ActivityTemplates)
                .HasForeignKey(x => x.InstructorAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymActivitySession>(entity =>
        {
            entity.ToTable("GymActivitySessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(150).IsRequired();
            entity.Property(x => x.InstructorName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Capacity).IsRequired();
            entity.Property(x => x.StartsAtUtc).IsRequired();
            entity.Property(x => x.EndsAtUtc).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.StartsAtUtc });
            entity.HasIndex(x => new { x.LocationId, x.StartsAtUtc });
            entity.HasIndex(x => new { x.GymActivityTemplateId, x.StartsAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.ActivitySessions)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Template)
                .WithMany(x => x.Sessions)
                .HasForeignKey(x => x.GymActivityTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.ActivitySessions)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.InstructorAssignment)
                .WithMany(x => x.ActivitySessions)
                .HasForeignKey(x => x.InstructorAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymActivityBooking>(entity =>
        {
            entity.ToTable("GymActivityBookings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.BookedAtUtc).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasIndex(x => new { x.GymActivitySessionId, x.GymMembershipId }).IsUnique();
            entity.HasIndex(x => new { x.GymId, x.BookedAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.ActivityBookings)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Session)
                .WithMany(x => x.Bookings)
                .HasForeignKey(x => x.GymActivitySessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Membership)
                .WithMany(x => x.ActivityBookings)
                .HasForeignKey(x => x.GymMembershipId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymLead>(entity =>
        {
            entity.ToTable("GymLeads");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.Property(x => x.Source).HasConversion<string>().IsRequired();
            entity.Property(x => x.Stage).HasConversion<string>().IsRequired();
            entity.Property(x => x.Interest).HasMaxLength(200);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.GymId, x.Stage, x.LocationId });
            entity.HasIndex(x => new { x.LocationId, x.NextFollowUpAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Leads)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.Leads)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.OwnerAssignment)
                .WithMany(x => x.OwnedLeads)
                .HasForeignKey(x => x.OwnerAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.ConvertedMembership)
                .WithMany(x => x.ConvertedLeads)
                .HasForeignKey(x => x.ConvertedMembershipId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymLeadTask>(entity =>
        {
            entity.ToTable("GymLeadTasks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymLeadId, x.Status, x.DueAtUtc });
            entity.HasIndex(x => new { x.GymId, x.Status, x.DueAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.LeadTasks)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Lead)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.GymLeadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.AssignedAssignment)
                .WithMany(x => x.LeadTasks)
                .HasForeignKey(x => x.AssignedAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymCampaign>(entity =>
        {
            entity.ToTable("GymCampaigns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Channel).HasConversion<string>().IsRequired();
            entity.Property(x => x.AudienceType).HasConversion<string>().IsRequired();
            entity.Property(x => x.TargetLeadStage).HasConversion<string>();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.Status, x.LocationId });
            entity.HasIndex(x => new { x.GymId, x.Channel, x.Status });
            entity.HasIndex(x => new { x.LocationId, x.ScheduledAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Campaigns)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.Campaigns)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.OwnerAssignment)
                .WithMany(x => x.Campaigns)
                .HasForeignKey(x => x.OwnerAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedCampaigns)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GymAutomationRule>(entity =>
        {
            entity.ToTable("GymAutomationRules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedByUserId).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Channel).HasConversion<string>().IsRequired();
            entity.Property(x => x.AudienceType).HasConversion<string>().IsRequired();
            entity.Property(x => x.TargetLeadStage).HasConversion<string>();
            entity.Property(x => x.ScheduleType).HasConversion<string>().IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.SubjectTemplate).HasMaxLength(180).IsRequired();
            entity.Property(x => x.MessageTemplate).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.NextRunAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.Status, x.LocationId });
            entity.HasIndex(x => new { x.GymId, x.Channel, x.Status });
            entity.HasIndex(x => new { x.LocationId, x.NextRunAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.AutomationRules)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.AutomationRules)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.OwnerAssignment)
                .WithMany(x => x.AutomationRules)
                .HasForeignKey(x => x.OwnerAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedAutomationRules)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GymWorkoutExercise>(entity =>
        {
            entity.ToTable("GymWorkoutExercises");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.MuscleGroup).HasMaxLength(80);
            entity.Property(x => x.Equipment).HasMaxLength(120);
            entity.Property(x => x.Instructions).HasMaxLength(2000);
            entity.Property(x => x.VideoUrl).HasMaxLength(256);
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.Name });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.WorkoutExercises)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymWorkoutTemplate>(entity =>
        {
            entity.ToTable("GymWorkoutTemplates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Goal).HasMaxLength(120);
            entity.Property(x => x.Level).HasConversion<string>().IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1500);
            entity.Property(x => x.DaysPerWeek).IsRequired();
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.LocationId, x.Name });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.WorkoutTemplates)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.WorkoutTemplates)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.CoachAssignment)
                .WithMany(x => x.WorkoutTemplates)
                .HasForeignKey(x => x.CoachAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymWorkoutTemplateItem>(entity =>
        {
            entity.ToTable("GymWorkoutTemplateItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DayNumber).IsRequired();
            entity.Property(x => x.SortOrder).IsRequired();
            entity.Property(x => x.ExerciseName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SetsPrescription).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RepetitionsPrescription).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Tempo).HasMaxLength(32);
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasIndex(x => new { x.GymWorkoutTemplateId, x.DayNumber, x.SortOrder }).IsUnique();

            entity
                .HasOne(x => x.Template)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.GymWorkoutTemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Exercise)
                .WithMany(x => x.TemplateItems)
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymWorkoutAssignment>(entity =>
        {
            entity.ToTable("GymWorkoutAssignments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedByUserId).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Goal).HasMaxLength(120);
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1500);
            entity.Property(x => x.AssignedAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.AssignedAtUtc });
            entity.HasIndex(x => new { x.GymMembershipId, x.AssignedAtUtc });
            entity.HasIndex(x => new { x.LocationId, x.AssignedAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.WorkoutAssignments)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Membership)
                .WithMany(x => x.WorkoutAssignments)
                .HasForeignKey(x => x.GymMembershipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.WorkoutAssignments)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.Template)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.GymWorkoutTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.CoachAssignment)
                .WithMany(x => x.WorkoutAssignments)
                .HasForeignKey(x => x.CoachAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedWorkoutAssignments)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GymWorkoutAssignmentItem>(entity =>
        {
            entity.ToTable("GymWorkoutAssignmentItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DayNumber).IsRequired();
            entity.Property(x => x.SortOrder).IsRequired();
            entity.Property(x => x.ExerciseName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SetsPrescription).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RepetitionsPrescription).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Tempo).HasMaxLength(32);
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasIndex(x => new { x.GymWorkoutAssignmentId, x.DayNumber, x.SortOrder }).IsUnique();

            entity
                .HasOne(x => x.Assignment)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.GymWorkoutAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Exercise)
                .WithMany(x => x.AssignmentItems)
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymWorkoutAssessment>(entity =>
        {
            entity.ToTable("GymWorkoutAssessments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RecordedByUserId).IsRequired();
            entity.Property(x => x.RecordedAtUtc).IsRequired();
            entity.Property(x => x.WeightKg).HasPrecision(6, 2);
            entity.Property(x => x.BodyFatPercentage).HasPrecision(5, 2);
            entity.Property(x => x.LeanMassKg).HasPrecision(6, 2);
            entity.Property(x => x.ChestCm).HasPrecision(6, 2);
            entity.Property(x => x.WaistCm).HasPrecision(6, 2);
            entity.Property(x => x.HipsCm).HasPrecision(6, 2);
            entity.Property(x => x.ArmCm).HasPrecision(6, 2);
            entity.Property(x => x.ThighCm).HasPrecision(6, 2);
            entity.Property(x => x.CalfCm).HasPrecision(6, 2);
            entity.Property(x => x.Notes).HasMaxLength(1500);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.RecordedAtUtc });
            entity.HasIndex(x => new { x.GymMembershipId, x.RecordedAtUtc });
            entity.HasIndex(x => new { x.LocationId, x.RecordedAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.WorkoutAssessments)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Membership)
                .WithMany(x => x.WorkoutAssessments)
                .HasForeignKey(x => x.GymMembershipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.WorkoutAssessments)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.CoachAssignment)
                .WithMany(x => x.WorkoutAssessments)
                .HasForeignKey(x => x.CoachAssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            entity
                .HasOne(x => x.RecordedByUser)
                .WithMany(x => x.RecordedWorkoutAssessments)
                .HasForeignKey(x => x.RecordedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GymSale>(entity =>
        {
            entity.ToTable("GymSales");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReferenceCode).HasMaxLength(32).IsRequired();
            entity.Property(x => x.CreatedByUserId).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.SoldAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.Property(x => x.SubtotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.PaidAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.RemainingAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.PaymentStatus).HasConversion<string>().IsRequired();

            entity.HasIndex(x => new { x.GymId, x.ReferenceCode }).IsUnique();
            entity.HasIndex(x => new { x.GymId, x.SoldAtUtc });
            entity.HasIndex(x => new { x.GymMembershipId, x.SoldAtUtc });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Membership)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.GymMembershipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedSales)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GymSaleCatalogItem>(entity =>
        {
            entity.ToTable("GymSaleCatalogItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ItemType).HasConversion<string>().IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.UnitPriceAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.DefaultQuantity).IsRequired();
            entity.Property(x => x.DefaultDiscountAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.IsActive).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();

            entity.HasIndex(x => new { x.GymId, x.LocationId, x.IsActive });

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.SaleCatalogItems)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Location)
                .WithMany(x => x.SaleCatalogItems)
                .HasForeignKey(x => x.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GymSaleLine>(entity =>
        {
            entity.ToTable("GymSaleLines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ItemType).HasConversion<string>().IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Quantity).IsRequired();
            entity.Property(x => x.UnitPriceAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.DiscountAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.LineTotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity
                .HasOne(x => x.Sale)
                .WithMany(x => x.Lines)
                .HasForeignKey(x => x.GymSaleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GymSalePayment>(entity =>
        {
            entity.ToTable("GymSalePayments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Method).HasConversion<string>().IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.ReceiptCode).HasMaxLength(40);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.Property(x => x.CreatedAtUtc).IsRequired();

            entity.HasIndex(x => x.ReceiptCode).IsUnique();

            entity
                .HasOne(x => x.Sale)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.GymSaleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TenantRoleAssignment>(entity =>
        {
            entity.ToTable("TenantRoleAssignments", table =>
            {
                table.HasCheckConstraint(
                    "CK_TenantRoleAssignments_ScopeConsistency",
                    "(\"ScopeType\" = 'Tenant' AND \"ScopeLocationId\" IS NULL) OR (\"ScopeType\" = 'Location' AND \"ScopeLocationId\" IS NOT NULL)");
                table.HasCheckConstraint(
                    "CK_TenantRoleAssignments_PrimaryOwnerScope",
                    "NOT \"IsPrimaryOwner\" OR (\"ScopeType\" = 'Tenant' AND \"ScopeLocationId\" IS NULL)");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.ScopeType).HasConversion<string>().IsRequired();
            entity.Property(x => x.IsPrimaryOwner).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().IsRequired();
            entity.Property(x => x.GrantedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.GymId, x.UserId, x.RoleId })
                .HasDatabaseName("IX_TenantRoleAssignments_TenantScopeUnique")
                .IsUnique()
                .HasFilter("\"ScopeType\" = 'Tenant' AND \"ScopeLocationId\" IS NULL AND \"Status\" = 'Active' AND \"RevokedAtUtc\" IS NULL");
            entity.HasIndex(x => new { x.GymId, x.UserId, x.RoleId, x.ScopeLocationId })
                .HasDatabaseName("IX_TenantRoleAssignments_LocationScopeUnique")
                .IsUnique()
                .HasFilter("\"ScopeType\" = 'Location' AND \"ScopeLocationId\" IS NOT NULL AND \"Status\" = 'Active' AND \"RevokedAtUtc\" IS NULL");
            entity.HasIndex(x => x.GymId)
                .HasDatabaseName("IX_TenantRoleAssignments_ActivePrimaryOwnerPerGym")
                .IsUnique()
                .HasFilter("\"IsPrimaryOwner\" AND \"Status\" = 'Active' AND \"RevokedAtUtc\" IS NULL");

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.StaffAssignments)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.User)
                .WithMany(x => x.TenantRoleAssignments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.StaffProfile)
                .WithMany(x => x.RoleAssignments)
                .HasForeignKey(x => x.StaffProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Role)
                .WithMany(x => x.StaffAssignments)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.ScopeLocation)
                .WithMany(x => x.StaffAssignments)
                .HasForeignKey(x => x.ScopeLocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<GymInvitation>(entity =>
        {
            entity.ToTable("GymInvitations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ExpiresAtUtc).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Invitations)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.GymMembership)
                .WithMany(x => x.Invitations)
                .HasForeignKey(x => x.GymMembershipId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuthenticationChallenge>(entity =>
        {
            entity.ToTable("AuthenticationChallenges", table =>
            {
                table.HasCheckConstraint(
                    "CK_AuthenticationChallenges_CodeExpiryByType",
                    "(\"Type\" = 'EmailVerification' AND \"CodeExpiresAtUtc\" IS NOT NULL) OR (\"Type\" <> 'EmailVerification' AND \"CodeExpiresAtUtc\" IS NULL)");
                table.HasCheckConstraint(
                    "CK_AuthenticationChallenges_CodeExpiryWithinSession",
                    "\"CodeExpiresAtUtc\" IS NULL OR \"CodeExpiresAtUtc\" <= \"SessionExpiresAtUtc\"");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.Type).HasConversion<string>().IsRequired();
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.CodeHash).HasMaxLength(128);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.SessionExpiresAtUtc).IsRequired();
            entity.Property(x => x.CodeExpiresAtUtc);
            entity.Property(x => x.LastSentAtUtc).IsRequired();
            entity.Property(x => x.AttemptCount).IsRequired();
            entity.Property(x => x.MaxAttempts).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.Type });

            entity
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PermissionCatalogItem>().HasData(PermissionCatalogSeed.GetCatalogItems());
    }
}
