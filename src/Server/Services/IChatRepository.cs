using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 聊天消息仓储接口
/// </summary>
public interface IChatRepository
{
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
}

/// <summary>
/// 内存聊天消息仓储实现
/// </summary>
public class InMemoryChatRepository : IChatRepository
{
    private readonly List<ChatMessage> _messages = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly int _maxMessages = 1000;

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
}
