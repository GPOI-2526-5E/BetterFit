using System.Text.Json.Serialization;

namespace Betterfit.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GymMembershipStatus
{
    PendingClaim = 0,
    Active = 1,
    Suspended = 2,
    Archived = 3
}
