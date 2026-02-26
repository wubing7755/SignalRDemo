namespace SignalRDemo.Domain.ValueObjects;

public class DisplayName : IEquatable<DisplayName>
{
    public string Value { get; }

    private DisplayName(string value) => Value = value;

    public static DisplayName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("显示名称不能为空", nameof(value));
        if (value.Length > 30)
            throw new ArgumentException("显示名称不能超过30个字符", nameof(value));
        
        return new DisplayName(value.Trim());
    }

    public static DisplayName? CreateOrDefault(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Create(value);

    public static DisplayName FromUserName(UserName userName) => new DisplayName(userName.Value);

    public bool Equals(DisplayName? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as DisplayName);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(DisplayName displayName) => displayName.Value;
}
