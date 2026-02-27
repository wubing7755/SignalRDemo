using System.Text.Json;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 消息缓存 DTO
/// </summary>
public class MessageCacheDto
{
    public string Id { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "Text";
    public string? MediaUrl { get; set; }
    public string? AltText { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 消息缓存服务接口
/// </summary>
public interface IMessageCacheService
{
    /// <summary>
    /// 将消息加入队列
    /// </summary>
    Task EnqueueAsync(MessageCacheDto message);

    /// <summary>
    /// 从队列取出消息
    /// </summary>
    Task<MessageCacheDto?> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// 获取实时消息缓存
    /// </summary>
    Task<MessageCacheDto?> GetAsync(string messageId);

    /// <summary>
    /// 设置实时消息缓存
    /// </summary>
    Task SetAsync(string messageId, MessageCacheDto message, TimeSpan expiry);

    /// <summary>
    /// 设置用户在线状态
    /// </summary>
    Task SetUserOnlineAsync(string userId);

    /// <summary>
    /// 设置用户离线状态
    /// </summary>
    Task SetUserOfflineAsync(string userId);

    /// <summary>
    /// 获取在线用户列表
    /// </summary>
    Task<List<string>> GetOnlineUsersAsync();

    /// <summary>
    /// 检查用户是否在线
    /// </summary>
    Task<bool> IsUserOnlineAsync(string userId);

    /// <summary>
    /// 获取队列长度
    /// </summary>
    Task<long> GetQueueLengthAsync();
}
