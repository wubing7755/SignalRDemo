using SignalRDemo.Shared.Models;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 文件消息持久化服务接口
/// </summary>
public interface IFileMessagePersistenceService
{
    /// <summary>
    /// 持久化单条消息
    /// </summary>
    Task PersistAsync(ChatMessage message);

    /// <summary>
    /// 批量持久化消息
    /// </summary>
    Task PersistBatchAsync(IEnumerable<ChatMessage> messages);

    /// <summary>
    /// 获取房间消息历史
    /// </summary>
    Task<List<ChatMessage>> GetRoomMessagesAsync(string roomId, int offset, int count);

    /// <summary>
    /// 获取房间消息数量
    /// </summary>
    Task<long> GetRoomMessageCountAsync(string roomId);

    /// <summary>
    /// 获取所有房间ID列表
    /// </summary>
    Task<List<string>> GetAllRoomIdsAsync();

    /// <summary>
    /// 重建索引
    /// </summary>
    Task RebuildIndexAsync();

    /// <summary>
    /// 归档旧消息
    /// </summary>
    Task ArchiveOldMessagesAsync(int daysToKeep);
}
