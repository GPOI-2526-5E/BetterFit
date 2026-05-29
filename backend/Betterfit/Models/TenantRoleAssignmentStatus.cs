using System.Text.Json.Serialization;

namespace Betterfit.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantRoleAssignmentStatus
{
    Active = 0,
    Suspended = 1,
    Revoked = 2
}
