using System.Text.Json.Serialization;

namespace Betterfit.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GymMembershipSource
{
    SelfSignup = 0,
    StaffInvite = 1,
    Import = 2,
    Migration = 3
}
