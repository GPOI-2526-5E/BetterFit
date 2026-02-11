using Betterfit.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Betterfit.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private static readonly Guid GymsReadPermissionId = Guid.Parse("1015f36b-e0c6-4cd7-88fc-3b2e25572c80");
    private static readonly Guid GymsWritePermissionId = Guid.Parse("9d56e44c-0117-4f5c-9c53-33257c380389");
    private static readonly Guid MembersReadPermissionId = Guid.Parse("32291f01-7fe4-44cf-9f1c-82bbf8a37295");
    private static readonly Guid MembersWritePermissionId = Guid.Parse("f2895883-e3b6-4f31-95ba-e8aa183d9a31");
    private static readonly Guid RolesReadPermissionId = Guid.Parse("aa09dba0-1f46-4197-a8e2-445c10d737f6");
    private static readonly Guid RolesWritePermissionId = Guid.Parse("f3dce9e8-6d98-40df-a312-3152f3248e09");
    private static readonly Guid BillingReadPermissionId = Guid.Parse("c7c46676-f643-4d17-b541-7d81dcbca635");
    private static readonly Guid BillingWritePermissionId = Guid.Parse("c04e4eef-c5f7-42b0-a820-c50a3978d19f");
    private static readonly Guid PlansApprovePermissionId = Guid.Parse("27ecb0f0-4862-45eb-9b9f-2ca9505855f4");
    private static readonly Guid ClassesReadPermissionId = Guid.Parse("4f0409b0-c0f2-47cb-94dd-722efcc5e847");
    private static readonly Guid ClassesWritePermissionId = Guid.Parse("c8e7699f-f64f-4578-9a8a-e486e3fd7f95");
    private static readonly Guid ReportsExportPermissionId = Guid.Parse("e00a4ee3-ec77-4030-8df3-31616b19226a");
    private static readonly Guid WorkoutsReadPermissionId = Guid.Parse("ea2b1144-a8f9-4f15-8afa-4fcf60ecaf50");
    private static readonly Guid WorkoutsWritePermissionId = Guid.Parse("132f89ad-c2de-4f84-93af-8619d30fca0d");
    private static readonly Guid ProfileReadPermissionId = Guid.Parse("de2a4fcb-13dd-4a8f-b435-5f1f0ca10961");
    private static readonly Guid ProfileWritePermissionId = Guid.Parse("93a83f38-69ef-4637-ae5f-7ec42b17dcb6");
    private static readonly Guid CheckinsApprovePermissionId = Guid.Parse("a5c55147-1d99-433e-b022-5164d7eff76d");

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Gym> Gyms => Set<Gym>();

    public DbSet<GymRole> GymRoles => Set<GymRole>();

    public DbSet<PermissionCatalogItem> PermissionCatalogItems => Set<PermissionCatalogItem>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<GymMembership> GymMemberships => Set<GymMembership>();

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

        builder.Entity<GymRole>(entity =>
        {
            entity.ToTable("GymRoles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.GymId).IsRequired();
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

        builder.Entity<PermissionCatalogItem>(entity =>
        {
            entity.ToTable("PermissionCatalogItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Resource).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedResource).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedAction).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);

            entity.HasIndex(x => new { x.NormalizedResource, x.NormalizedAction }).IsUnique();
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

        builder.Entity<GymMembership>(entity =>
        {
            entity.ToTable("GymMemberships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).IsRequired();
            entity.Property(x => x.AssignedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.GymId }).IsUnique();

            entity
                .HasOne(x => x.User)
                .WithMany(x => x.GymMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Gym)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.GymId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(x => x.Role)
                .WithMany(x => x.GymMemberships)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PermissionCatalogItem>().HasData(
            CreateCatalogItem(GymsReadPermissionId, "gyms", "read", "View gym records"),
            CreateCatalogItem(GymsWritePermissionId, "gyms", "write", "Create or update gym records"),
            CreateCatalogItem(MembersReadPermissionId, "members", "read", "View member data"),
            CreateCatalogItem(MembersWritePermissionId, "members", "write", "Create or update members"),
            CreateCatalogItem(RolesReadPermissionId, "roles", "read", "View role definitions"),
            CreateCatalogItem(RolesWritePermissionId, "roles", "write", "Create or update role definitions"),
            CreateCatalogItem(BillingReadPermissionId, "billing", "read", "View billing information"),
            CreateCatalogItem(BillingWritePermissionId, "billing", "write", "Modify billing information"),
            CreateCatalogItem(PlansApprovePermissionId, "plans", "approve", "Approve plans and subscriptions"),
            CreateCatalogItem(ClassesReadPermissionId, "classes", "read", "View class schedules"),
            CreateCatalogItem(ClassesWritePermissionId, "classes", "write", "Create or update class schedules"),
            CreateCatalogItem(ReportsExportPermissionId, "reports", "export", "Export reports"),
            CreateCatalogItem(WorkoutsReadPermissionId, "workouts", "read", "View workout plans"),
            CreateCatalogItem(WorkoutsWritePermissionId, "workouts", "write", "Create or update workout plans"),
            CreateCatalogItem(ProfileReadPermissionId, "profile", "read", "View own profile"),
            CreateCatalogItem(ProfileWritePermissionId, "profile", "write", "Update own profile"),
            CreateCatalogItem(CheckinsApprovePermissionId, "checkins", "approve", "Approve member check-ins")
        );
    }

    private static PermissionCatalogItem CreateCatalogItem(Guid id, string resource, string action, string description)
    {
        return new PermissionCatalogItem
        {
            Id = id,
            Resource = resource,
            Action = action,
            NormalizedResource = resource.ToUpperInvariant(),
            NormalizedAction = action.ToUpperInvariant(),
            Description = description
        };
    }

    public static IReadOnlyList<(string Name, string? Description, IReadOnlyCollection<(string Resource, string Action)> Permissions)> GetDefaultRoleTemplates()
    {
        return
        [
            (
                "Owner",
                "Full access to all CRM capabilities in a gym.",
                new List<(string Resource, string Action)>
                {
                    ("gyms", "read"),
                    ("gyms", "write"),
                    ("members", "read"),
                    ("members", "write"),
                    ("roles", "read"),
                    ("roles", "write"),
                    ("billing", "read"),
                    ("billing", "write"),
                    ("plans", "approve")
                }
            ),
            (
                "Manager",
                "Manages members, classes, and day-to-day operations.",
                new List<(string Resource, string Action)>
                {
                    ("gyms", "read"),
                    ("members", "read"),
                    ("members", "write"),
                    ("classes", "read"),
                    ("classes", "write"),
                    ("reports", "export")
                }
            ),
            (
                "Trainer",
                "Handles training plans and member coaching.",
                new List<(string Resource, string Action)>
                {
                    ("members", "read"),
                    ("workouts", "read"),
                    ("workouts", "write")
                }
            ),
            (
                "Member",
                "Basic gym member access.",
                new List<(string Resource, string Action)>
                {
                    ("workouts", "read"),
                    ("profile", "read"),
                    ("profile", "write")
                }
            )
        ];
    }
}
