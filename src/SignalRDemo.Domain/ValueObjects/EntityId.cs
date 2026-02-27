namespace SignalRDemo.Domain.ValueObjects;

/// <summary>
/// 强类型ID基类 - 泛型版本
/// </summary>
public abstract class EntityId<T> : IEquatable<EntityId<T>> where T : EntityId<T>
{
    public string Value { get; }

    protected EntityId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ID不能为空", nameof(value));
        Value = value;
    }

    public static TId Create<TId>(string value) where TId : EntityId<T>
    {
        var constructor = typeof(TId).GetConstructor(new[] { typeof(string) });
        if (constructor == null)
            throw new InvalidOperationException($"类型 {typeof(TId).Name} 没有接受 string 的构造函数");
        return (TId)constructor.Invoke(new[] { value });
    }

    public bool Equals(EntityId<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is EntityId<T> other && Equals(other);
    }

    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => $"{typeof(T).Name}: {Value}";

    public static bool operator ==(EntityId<T>? left, EntityId<T>? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(EntityId<T>? left, EntityId<T>? right)
        => !(left == right);
}
