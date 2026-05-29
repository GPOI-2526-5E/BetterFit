using System.Text.Json.Serialization;

namespace Betterfit.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthenticationChallengeType
{
    EmailVerification = 0,
    TwoFactorSetup = 1,
    TwoFactorLogin = 2
}
