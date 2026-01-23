using System.Security.Cryptography;

namespace Infrastructure.Utils;

public static class EncryptionHelper
{
    /// <summary>
    /// Generates a short but cryptographically secure key.
    /// Uses Base64Url encoding to produce URL-safe characters.
    /// </summary>
    /// <param name="byteLength">Number of random bytes (default 16 = 22 chars output)</param>
    /// <returns>A URL-safe random string</returns>
    public static string GenerateSecureKey(int byteLength = 16)
    {
        var randomBytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
