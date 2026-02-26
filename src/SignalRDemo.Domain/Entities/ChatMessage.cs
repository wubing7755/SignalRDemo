using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Domain.Entities;

public class ChatMessage
{
    public MessageId Id { get; private set; } = null!;
    public UserId UserId { get; private set; } = null!;
    public UserName UserName { get; private set; } = null!;
    public DisplayName DisplayName { get; private set; } = null!;
    public RoomId RoomId { get; private set; } = null!;
    public string Content { get; private set; } = string.Empty;
    public string MessageType { get; private set; } = "Text";
    public string? MediaUrl { get; private set; }
    public string? AltText { get; private set; }
    public DateTime Timestamp { get; private set; }

    // 私有构造函数
    private ChatMessage() { }

    public static ChatMessage Create(
        UserId userId,
        UserName userName,
        DisplayName displayName,
        RoomId roomId,
        string content,
        string messageType = "Text",
        string? mediaUrl = null,
        string? altText = null)
    {
        return new ChatMessage
        {
            Id = MessageId.New(),
            UserId = userId,
            UserName = userName,
            DisplayName = displayName,
            RoomId = roomId,
            Content = content,
            MessageType = messageType,
            MediaUrl = mediaUrl,
            AltText = altText,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ChatMessage Reconstitute(
        MessageId id,
        UserId userId,
        UserName userName,
        DisplayName displayName,
        RoomId roomId,
        string content,
        string messageType,
        string? mediaUrl,
        string? altText,
        DateTime timestamp)
    {
        return new ChatMessage
        {
            Id = id,
            UserId = userId,
            UserName = userName,
            DisplayName = displayName,
            RoomId = roomId,
            Content = content,
            MessageType = messageType,
            MediaUrl = mediaUrl,
            AltText = altText,
            Timestamp = timestamp
        };
    }
}
