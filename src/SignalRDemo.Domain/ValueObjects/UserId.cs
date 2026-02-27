namespace SignalRDemo.Domain.ValueObjects;

public class UserId : EntityId<UserId>
{
    public UserId(string value) : base(value) { }

    public static UserId New() => new UserId(Guid.NewGuid().ToString());
    public static UserId Create(string value) => new UserId(value);
    public static UserId From(string value) => new UserId(value);
}
