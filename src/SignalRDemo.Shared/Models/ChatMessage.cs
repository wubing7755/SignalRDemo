namespace SignalRDemo.Shared.Models;

/// <summary>
/// 聊天消息
/// </summary>
public class ChatMessage
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? User { get; set; } // 兼容旧代码 - 使用时优先使用UserName
    public string DisplayName { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty; // 兼容旧版
    public MessageType Type { get; set; } = MessageType.Text;
    public string TypeString => Type.ToString(); // 兼容字符串类型
    public string? MediaUrl { get; set; }
    public string? AltText { get; set; }
    public DateTime Timestamp { get; set; }
}
