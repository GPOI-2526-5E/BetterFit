namespace Betterfit.Authorization;

public static class AuthorizationPolicies
{
    public const string GymsRead = "GymPermission.Gyms.Read";
    public const string GymsWrite = "GymPermission.Gyms.Write";
    public const string LocationsRead = "GymPermission.Locations.Read";
    public const string LocationsWrite = "GymPermission.Locations.Write";
    public const string MembersRead = "GymPermission.Members.Read";
    public const string MembersWrite = "GymPermission.Members.Write";
    public const string CrmRead = "GymPermission.Crm.Read";
    public const string CrmWrite = "GymPermission.Crm.Write";
    public const string StaffAssignmentsRead = "GymPermission.StaffAssignments.Read";
    public const string StaffAssignmentsWrite = "GymPermission.StaffAssignments.Write";
    public const string RolesRead = "GymPermission.Roles.Read";
    public const string RolesWrite = "GymPermission.Roles.Write";
    public const string BillingRead = "GymPermission.Billing.Read";
    public const string BillingWrite = "GymPermission.Billing.Write";
    public const string ClassesRead = "GymPermission.Classes.Read";
    public const string ClassesWrite = "GymPermission.Classes.Write";
    public const string ReportsRead = "GymPermission.Reports.Read";
    public const string WorkoutsRead = "GymPermission.Workouts.Read";
    public const string WorkoutsWrite = "GymPermission.Workouts.Write";
    public const string CheckinsApprove = "GymPermission.Checkins.Approve";
    public const string SecurityPolicyRead = "GymPermission.SecurityPolicy.Read";
    public const string SecurityPolicyWrite = "GymPermission.SecurityPolicy.Write";
    public const string IntegrationsRead = "GymPermission.Integrations.Read";
    public const string IntegrationsWrite = "GymPermission.Integrations.Write";
}
