using System.Text.Json.Serialization;

namespace Betterfit.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantRoleAssignmentScopeType
{
    Tenant = 0,
    Location = 1
}
