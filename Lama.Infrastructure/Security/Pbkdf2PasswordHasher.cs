using System.Security.Cryptography;
using Lama.Application.AccessControl;

namespace Lama.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256 with a per-password random salt, stored as
/// "pbkdf2-sha256.&lt;iterations&gt;.&lt;salt&gt;.&lt;hash&gt;" — the iteration count travels with the
/// hash, so it can be raised later without invalidating existing passwords.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int Iterations = 210_000; // OWASP recommendation for PBKDF2-SHA256
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(password, salt, Iterations);

        return string.Join('.', Algorithm, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
            return false;

        var parts = hash.Split('.');
        if (parts.Length != 4 || parts[0] != Algorithm || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
            return false;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Derive(password, salt, iterations, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int size = HashSize) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, size);
}
