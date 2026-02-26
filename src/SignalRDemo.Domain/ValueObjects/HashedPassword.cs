using System.Security.Cryptography;

namespace SignalRDemo.Domain.ValueObjects;

public class HashedPassword : IEquatable<HashedPassword>
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int HashIterations = 100000;

    public string Value { get; }

    private HashedPassword(string value) => Value = value;

    public static HashedPassword Create(byte[] salt, byte[] hash)
    {
        var saltHex = Convert.ToHexString(salt);
        var hashHex = Convert.ToHexString(hash);
        return new HashedPassword($"{saltHex}:{hashHex}");
    }

    public static HashedPassword From(string hashedValue) => new HashedPassword(hashedValue);

    public bool Verify(Password password)
    {
        try
        {
            var parts = Value.Split(':');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromHexString(parts[0]);
            var storedHash = Convert.FromHexString(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password.Value, salt, HashIterations, HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(HashSize);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(HashedPassword? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as HashedPassword);
    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => "[HASHED_PASSWORD]";
}
