namespace SignalRDemo.Domain.ValueObjects;

public class MessageId : EntityId
{
    public MessageId(string value) : base(value) { }

    public static MessageId New() => new MessageId(Guid.NewGuid().ToString());
    public static MessageId Create(string value) => new MessageId(value);
    public static MessageId From(string value) => new MessageId(value);
}
