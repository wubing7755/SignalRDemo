namespace SignalRDemo.Shared.Models;

/// <summary>
/// 用户连接模型 - 表示一个在线用户的连接信息
/// </summary>
/// <remarks>
/// <para>用途：记录用户连接状态的轻量级数据模型</para>
/// <para>使用场景：用于追踪在线用户列表和用户状态管理</para>
/// <para>与 SignalR 的 Context.ConnectionId 对应</para>
/// </remarks>
public class UserConnection
{
    /// <summary>
    /// SignalR 连接 ID
    /// </summary>
    /// <remarks>
    /// SignalR 为每个客户端连接自动生成的唯一标识符
    /// </remarks>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// 用户显示名称
    /// </summary>
    /// <remarks>
    /// 用户在聊天中显示的名称
    /// 可以是默认生成的（如 "User_xxxx"）或用户自定义的
    /// </remarks>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 用户连接成功的时间（UTC 时间）
    /// </summary>
    /// <remarks>
    /// 记录用户建立连接的时间点
    /// 可用于显示用户在线时长
    /// </remarks>
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}
