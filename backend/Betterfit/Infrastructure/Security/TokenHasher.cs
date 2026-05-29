using System.Security.Cryptography;
using System.Text;

namespace Betterfit.Infrastructure.Security;

/// <summary>
/// Shared utility for hashing invitation tokens.
/// </summary>
public static class TokenHasher
{
    public static string ComputeTokenHash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
