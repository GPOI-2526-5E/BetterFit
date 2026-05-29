using Betterfit.Models;

namespace Betterfit.Data;

/// <summary>
/// Permission catalog seed data and default role templates.
/// Extracted from AppDbContext to keep the DbContext focused on schema configuration.
/// </summary>
public static class PermissionCatalogSeed
{
    public static readonly Guid GymsReadPermissionId = Guid.Parse("1015f36b-e0c6-4cd7-88fc-3b2e25572c80");
    public static readonly Guid GymsWritePermissionId = Guid.Parse("9d56e44c-0117-4f5c-9c53-33257c380389");
    public static readonly Guid MembersReadPermissionId = Guid.Parse("32291f01-7fe4-44cf-9f1c-82bbf8a37295");
    public static readonly Guid MembersWritePermissionId = Guid.Parse("f2895883-e3b6-4f31-95ba-e8aa183d9a31");
    public static readonly Guid CrmReadPermissionId = Guid.Parse("99727c60-2d9b-4c92-bc55-d1cc633a09f9");
    public static readonly Guid CrmWritePermissionId = Guid.Parse("75d7dd0d-9997-4f7a-8258-8a98b2e8717f");
    public static readonly Guid StaffAssignmentsReadPermissionId = Guid.Parse("f4146dab-43bc-494c-86d2-c0256744996a");
    public static readonly Guid StaffAssignmentsWritePermissionId = Guid.Parse("98b12021-bad0-49ec-bd1f-2d8d4e9b3f81");
    public static readonly Guid RolesReadPermissionId = Guid.Parse("aa09dba0-1f46-4197-a8e2-445c10d737f6");
    public static readonly Guid RolesWritePermissionId = Guid.Parse("f3dce9e8-6d98-40df-a312-3152f3248e09");
    public static readonly Guid BillingReadPermissionId = Guid.Parse("c7c46676-f643-4d17-b541-7d81dcbca635");
    public static readonly Guid BillingWritePermissionId = Guid.Parse("c04e4eef-c5f7-42b0-a820-c50a3978d19f");
    public static readonly Guid PlansApprovePermissionId = Guid.Parse("27ecb0f0-4862-45eb-9b9f-2ca9505855f4");
    public static readonly Guid ClassesReadPermissionId = Guid.Parse("4f0409b0-c0f2-47cb-94dd-722efcc5e847");
    public static readonly Guid ClassesWritePermissionId = Guid.Parse("c8e7699f-f64f-4578-9a8a-e486e3fd7f95");
    public static readonly Guid ReportsReadPermissionId = Guid.Parse("7689fa0a-38ae-4d10-b8fc-e6db96ca15ec");
    public static readonly Guid ReportsExportPermissionId = Guid.Parse("e00a4ee3-ec77-4030-8df3-31616b19226a");
    public static readonly Guid WorkoutsReadPermissionId = Guid.Parse("ea2b1144-a8f9-4f15-8afa-4fcf60ecaf50");
    public static readonly Guid WorkoutsWritePermissionId = Guid.Parse("132f89ad-c2de-4f84-93af-8619d30fca0d");
    public static readonly Guid ProfileReadPermissionId = Guid.Parse("de2a4fcb-13dd-4a8f-b435-5f1f0ca10961");
    public static readonly Guid ProfileWritePermissionId = Guid.Parse("93a83f38-69ef-4637-ae5f-7ec42b17dcb6");
    public static readonly Guid CheckinsApprovePermissionId = Guid.Parse("a5c55147-1d99-433e-b022-5164d7eff76d");
    public static readonly Guid LocationsReadPermissionId = Guid.Parse("d88d6e31-2f85-4d05-a9bc-802eec0e7846");
    public static readonly Guid LocationsWritePermissionId = Guid.Parse("0995f6e7-d10b-486c-8633-b1a6140ea170");
    public static readonly Guid SecurityPolicyReadPermissionId = Guid.Parse("6fcf5996-b724-44aa-bab7-7754505dcf3e");
    public static readonly Guid SecurityPolicyWritePermissionId = Guid.Parse("54e4a2dd-8e6d-4919-b7d6-dd5b7b8ed5f7");
    public static readonly Guid IntegrationsReadPermissionId = Guid.Parse("bbd8883e-d299-41ef-8fd7-e39db8a9dab4");
    public static readonly Guid IntegrationsWritePermissionId = Guid.Parse("5adf6f11-4129-4333-a76e-a4c752d8fd54");

    public static PermissionCatalogItem[] GetCatalogItems()
    {
        return
        [
            CreateCatalogItem(GymsReadPermissionId, "gyms", "read", "READ_GYMS"),
            CreateCatalogItem(GymsWritePermissionId, "gyms", "write", "WRITE_GYMS"),
            CreateCatalogItem(MembersReadPermissionId, "members", "read", "READ_MEMBERS"),
            CreateCatalogItem(MembersWritePermissionId, "members", "write", "WRITE_MEMBERS"),
            CreateCatalogItem(CrmReadPermissionId, "crm", "read", "READ_CRM"),
            CreateCatalogItem(CrmWritePermissionId, "crm", "write", "WRITE_CRM"),
            CreateCatalogItem(StaffAssignmentsReadPermissionId, "staff_assignments", "read", "READ_STAFF_ASSIGNMENTS"),
            CreateCatalogItem(StaffAssignmentsWritePermissionId, "staff_assignments", "write", "WRITE_STAFF_ASSIGNMENTS"),
            CreateCatalogItem(RolesReadPermissionId, "roles", "read", "READ_ROLES"),
            CreateCatalogItem(RolesWritePermissionId, "roles", "write", "WRITE_ROLES"),
            CreateCatalogItem(BillingReadPermissionId, "billing", "read", "READ_BILLING"),
            CreateCatalogItem(BillingWritePermissionId, "billing", "write", "WRITE_BILLING"),
            CreateCatalogItem(PlansApprovePermissionId, "plans", "approve", "APPROVE_PLANS"),
            CreateCatalogItem(ClassesReadPermissionId, "classes", "read", "READ_CLASSES"),
            CreateCatalogItem(ClassesWritePermissionId, "classes", "write", "WRITE_CLASSES"),
            CreateCatalogItem(ReportsReadPermissionId, "reports", "read", "READ_REPORTS"),
            CreateCatalogItem(ReportsExportPermissionId, "reports", "export", "EXPORT_REPORTS"),
            CreateCatalogItem(WorkoutsReadPermissionId, "workouts", "read", "READ_WORKOUTS"),
            CreateCatalogItem(WorkoutsWritePermissionId, "workouts", "write", "WRITE_WORKOUTS"),
            CreateCatalogItem(ProfileReadPermissionId, "profile", "read", "READ_PROFILE"),
            CreateCatalogItem(ProfileWritePermissionId, "profile", "write", "WRITE_PROFILE"),
            CreateCatalogItem(CheckinsApprovePermissionId, "checkins", "approve", "APPROVE_CHECKINS"),
            CreateCatalogItem(LocationsReadPermissionId, "locations", "read", "READ_LOCATIONS"),
            CreateCatalogItem(LocationsWritePermissionId, "locations", "write", "WRITE_LOCATIONS"),
            CreateCatalogItem(SecurityPolicyReadPermissionId, "security_policy", "read", "READ_SECURITY_POLICY"),
            CreateCatalogItem(SecurityPolicyWritePermissionId, "security_policy", "write", "WRITE_SECURITY_POLICY"),
            CreateCatalogItem(IntegrationsReadPermissionId, "integrations", "read", "READ_INTEGRATIONS"),
            CreateCatalogItem(IntegrationsWritePermissionId, "integrations", "write", "WRITE_INTEGRATIONS"),
        ];
    }

    public static IReadOnlyList<(string Name, string? Description, IReadOnlyCollection<(string Resource, string Action)> Permissions)> GetDefaultRoleTemplates()
    {
        return
        [
            (
                "Owner",
                "Full access to all tenant capabilities.",
                new List<(string Resource, string Action)>
                {
                    ("gyms", "read"),
                    ("gyms", "write"),
                    ("locations", "read"),
                    ("locations", "write"),
                    ("members", "read"),
                    ("members", "write"),
                    ("crm", "read"),
                    ("crm", "write"),
                    ("staff_assignments", "read"),
                    ("staff_assignments", "write"),
                    ("roles", "read"),
                    ("roles", "write"),
                    ("security_policy", "read"),
                    ("security_policy", "write"),
                    ("integrations", "read"),
                    ("integrations", "write"),
                    ("billing", "read"),
                    ("billing", "write"),
                    ("classes", "read"),
                    ("classes", "write"),
                    ("reports", "read"),
                    ("workouts", "read"),
                    ("workouts", "write"),
                    ("plans", "approve"),
                    ("checkins", "approve"),
                    ("reports", "export")
                }
            ),
            (
                "Manager",
                "Manages members, classes, and day-to-day operations.",
                new List<(string Resource, string Action)>
                {
                    ("gyms", "read"),
                    ("locations", "read"),
                    ("members", "read"),
                    ("members", "write"),
                    ("crm", "read"),
                    ("crm", "write"),
                    ("staff_assignments", "read"),
                    ("security_policy", "read"),
                    ("integrations", "read"),
                    ("integrations", "write"),
                    ("billing", "read"),
                    ("billing", "write"),
                    ("checkins", "approve"),
                    ("classes", "read"),
                    ("classes", "write"),
                    ("reports", "read"),
                    ("workouts", "read"),
                    ("workouts", "write"),
                    ("reports", "export")
                }
            ),
            (
                "Reception",
                "Handles front-desk onboarding, check-ins, and daily member support.",
                new List<(string Resource, string Action)>
                {
                    ("locations", "read"),
                    ("members", "read"),
                    ("members", "write"),
                    ("crm", "read"),
                    ("crm", "write"),
                    ("billing", "read"),
                    ("billing", "write"),
                    ("checkins", "approve"),
                    ("profile", "read"),
                    ("profile", "write")
                }
            ),
            (
                "Coach",
                "Handles training plans and member coaching.",
                new List<(string Resource, string Action)>
                {
                    ("members", "read"),
                    ("workouts", "read"),
                    ("workouts", "write"),
                    ("profile", "read")
                }
            ),
            (
                "Support Operator",
                "Technical or support operator with limited operational access.",
                new List<(string Resource, string Action)>
                {
                    ("gyms", "read"),
                    ("locations", "read"),
                    ("members", "read"),
                    ("reports", "read"),
                    ("integrations", "read")
                }
            )
        ];
    }

    private static PermissionCatalogItem CreateCatalogItem(Guid id, string resource, string action, string descriptionKey)
    {
        return new PermissionCatalogItem
        {
            Id = id,
            Resource = resource,
            Action = action,
            NormalizedResource = resource.ToUpperInvariant(),
            NormalizedAction = action.ToUpperInvariant(),
            DescriptionKey = descriptionKey
        };
    }
}
