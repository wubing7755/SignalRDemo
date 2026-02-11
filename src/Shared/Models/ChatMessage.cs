namespace SignalRDemo.Shared.Models;

/// <summary>
/// 聊天消息模型 - 表示一条完整的聊天消息
/// </summary>
/// <remarks>
/// <para>用途：前后端共用的消息数据结构，用于在 SignalR 通信中传递消息内容</para>
/// <para>使用场景：当客户端发送消息或服务器广播消息时使用此模型</para>
/// <para>序列化：使用 JSON 序列化在前端和后端之间传输</para>
/// </remarks>
public class ChatMessage
{
    /// <summary>
    /// 发送消息的用户名
    /// </summary>
    /// <remarks>
    /// 标识消息的发送者，便于区分不同用户的消息
    /// </remarks>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// 消息的具体内容
    /// </summary>
    /// <remarks>
    /// 用户输入的聊天文本内容
    /// </remarks>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 消息发送的时间戳（UTC 时间）
    /// </summary>
    /// <remarks>
    /// 记录消息创建的时间，用于显示消息发送时间
    /// 默认值为 DateTime.UtcNow（服务器时间）
    /// 注意：服务器端会覆盖此值以确保时间准确性
    /// </remarks>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
