namespace SignalRDemo.Domain.ValueObjects;

public class RoomName : IEquatable<RoomName>
{
    public string Value { get; }

    private RoomName(string value) => Value = value;

    public static RoomName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("房间名称不能为空", nameof(value));
        if (value.Length < 2 || value.Length > 50)
            throw new ArgumentException("房间名称长度必须在2-50个字符之间", nameof(value));

        return new RoomName(value.Trim());
    }

    public static RoomName? CreateOrDefault(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : Create(value);

    public bool Equals(RoomName? other)
    {
        if (other is null) return false;
        return Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as RoomName);
    public override int GetHashCode() => Value.ToLowerInvariant().GetHashCode();
    public override string ToString() => Value;

    public static implicit operator string(RoomName roomName) => roomName.Value;
}
