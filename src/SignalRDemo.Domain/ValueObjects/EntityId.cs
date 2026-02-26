namespace SignalRDemo.Domain.ValueObjects;

public abstract class EntityId : IEquatable<EntityId>
{
    public string Value { get; }

    protected EntityId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ID不能为空", nameof(value));
        Value = value;
    }

    public static EntityId Create(string value) => throw new NotImplementedException("请使用具体类型的Create方法");

    public static TId Create<TId>(string value) where TId : EntityId
    {
        var constructor = typeof(TId).GetConstructor(new[] { typeof(string) });
        if (constructor == null)
            throw new InvalidOperationException($"类型 {typeof(TId).Name} 没有接受 string 的构造函数");
        return (TId)constructor.Invoke(new object[] { value });
    }

    public bool Equals(EntityId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj) => Equals(obj as EntityId);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    public static bool operator ==(EntityId? left, EntityId? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(EntityId? left, EntityId? right) => !(left == right);
}
