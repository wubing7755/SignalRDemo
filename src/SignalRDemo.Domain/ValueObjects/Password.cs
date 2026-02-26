using System.Security.Cryptography;
using System.Text;

namespace SignalRDemo.Domain.ValueObjects;

public class Password : IEquatable<Password>
{
    private const int SaltSize = 16;
    private const int HashIterations = 100000;
    
    public string Value { get; }

    private Password(string value) => Value = value;

    public static Password Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("密码不能为空", nameof(value));
        if (value.Length < 6)
            throw new ArgumentException("密码至少需要6个字符", nameof(value));
        
        return new Password(value);
    }

    public HashedPassword Hash()
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[SaltSize];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(Value, salt, HashIterations, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        return HashedPassword.Create(salt, hash);
    }

    public bool Equals(Password? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as Password);
    public override int GetHashCode() => Value.GetHashCode();
}
