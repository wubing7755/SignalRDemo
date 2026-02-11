namespace SignalRDemo.Shared.Models;

/// <summary>
/// 用户连接模型 - 表示一个在线用户的连接信息
/// </summary>
public class UserConnection
{
    /// <summary>
    /// SignalR 连接 ID
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID（登录后设置）
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 用户显示名称
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 用户连接成功的时间（UTC 时间）
    /// </summary>
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
}
