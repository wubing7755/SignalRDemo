using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 聊天消息仓储接口
/// </summary>
public interface IChatRepository
{
    // ========== 通用消息 ==========
    
    /// <summary>
    /// 保存消息
    /// </summary>
    Task SaveMessageAsync(ChatMessage message);
    
    /// <summary>
    /// 获取最近的消息
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(int count = 50);
    
    /// <summary>
    /// 获取消息总数
    /// </summary>
    Task<int> GetMessageCountAsync();

    // ========== 房间消息 ==========
    
    /// <summary>
    /// 保存房间消息
    /// </summary>
    Task SaveRoomMessageAsync(ChatMessage message);
    
    /// <summary>
    /// 获取房间消息历史
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetRoomMessagesAsync(string roomId, int count = 50);
    
    /// <summary>
    /// 获取房间消息数量
    /// </summary>
    Task<int> GetRoomMessageCountAsync(string roomId);
    
    /// <summary>
    /// 获取所有房间的消息统计
    /// </summary>
    Task<Dictionary<string, int>> GetAllRoomMessageCountsAsync();
}

/// <summary>
/// 内存聊天消息仓储实现
/// </summary>
public class InMemoryChatRepository : IChatRepository
{
    private readonly List<ChatMessage> _messages = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly int _maxMessages = 1000;
    private readonly int _maxMessagesPerRoom = 500;

    public Task SaveMessageAsync(ChatMessage message)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Add(message);
            
            // 限制最大消息数，防止内存无限增长
            if (_messages.Count > _maxMessages)
            {
                _messages.RemoveAt(0);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(int count = 50)
    {
        _lock.EnterReadLock();
        try
        {
            var result = _messages
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToList()
                .AsReadOnly();
            
            return Task.FromResult<IReadOnlyList<ChatMessage>>(result);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<int> GetMessageCountAsync()
    {
        _lock.EnterReadLock();
        try
        {
            return Task.FromResult(_messages.Count);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // ========== 房间消息实现 ==========

    public Task SaveRoomMessageAsync(ChatMessage message)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Add(message);
            
            // 限制每个房间的最大消息数
            var roomMessages = _messages.Where(m => m.RoomId == message.RoomId).ToList();
            if (roomMessages.Count > _maxMessagesPerRoom)
            {
                var oldestMessages = roomMessages
                    .OrderBy(m => m.Timestamp)
                    .Take(roomMessages.Count - _maxMessagesPerRoom);
                
                foreach (var msg in oldestMessages.ToList())
                {
                    _messages.Remove(msg);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatMessage>> GetRoomMessagesAsync(string roomId, int count = 50)
    {
        _lock.EnterReadLock();
        try
        {
            var roomMessages = _messages
                .Where(m => m.RoomId == roomId)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToList()
                .AsReadOnly();
            
            return Task.FromResult<IReadOnlyList<ChatMessage>>(roomMessages);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<int> GetRoomMessageCountAsync(string roomId)
    {
        _lock.EnterReadLock();
        try
        {
            var count = _messages.Count(m => m.RoomId == roomId);
            return Task.FromResult(count);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<Dictionary<string, int>> GetAllRoomMessageCountsAsync()
    {
        _lock.EnterReadLock();
        try
        {
            var counts = new Dictionary<string, int>();
            foreach (var msg in _messages)
            {
                if (!counts.ContainsKey(msg.RoomId))
                {
                    counts[msg.RoomId] = 0;
                }
                counts[msg.RoomId]++;
            }
            return Task.FromResult(counts);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
