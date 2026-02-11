namespace SignalRDemo.Shared.Models;

/// <summary>
/// 聊天消息模型 - 表示一条完整的聊天消息
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 消息唯一ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 发送消息的用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 发送消息的用户名（兼容性属性）
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 发送消息的用户名（兼容旧版本）
    /// </summary>
    public string User
    {
        get => UserName;
        set => UserName = value;
    }

    /// <summary>
    /// 发送消息的用户显示昵称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 房间ID
    /// </summary>
    public string RoomId { get; set; } = string.Empty;

    /// <summary>
    /// 消息的具体内容
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 消息类型
    /// </summary>
    public MessageType Type { get; set; } = MessageType.Text;

    /// <summary>
    /// 媒体URL（图片、文件等）
    /// </summary>
    public string? MediaUrl { get; set; }

    /// <summary>
    /// 替代文本
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// 消息发送的时间戳（UTC 时间）
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
