namespace Betterfit.Authorization;

public static class AuthorizationPolicies
{
    public const string GymsRead = "GymPermission.Gyms.Read";
    public const string GymsWrite = "GymPermission.Gyms.Write";
    public const string MembersRead = "GymPermission.Members.Read";
    public const string MembersWrite = "GymPermission.Members.Write";
    public const string RolesRead = "GymPermission.Roles.Read";
    public const string RolesWrite = "GymPermission.Roles.Write";
}
