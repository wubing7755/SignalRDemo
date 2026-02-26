namespace SignalRDemo.Domain.ValueObjects;

public class UserName : IEquatable<UserName>
{
    public string Value { get; }

    private UserName(string value) => Value = value;

    public static UserName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("用户名不能为空", nameof(value));
        if (value.Length < 3 || value.Length > 20)
            throw new ArgumentException("用户名长度必须在3-20个字符之间", nameof(value));
        
        return new UserName(value.Trim());
    }

    public static UserName? CreateOrDefault(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Create(value);

    public bool Equals(UserName? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as UserName);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(UserName userName) => userName.Value;
}
