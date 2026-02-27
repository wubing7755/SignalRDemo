namespace SignalRDemo.Domain.ValueObjects;

public class RoomId : EntityId<RoomId>
{
    public RoomId(string value) : base(value) { }

    public static RoomId New() => new RoomId(Guid.NewGuid().ToString());
    public static RoomId Create(string value) => new RoomId(value);
    public static RoomId From(string value) => new RoomId(value);
}
