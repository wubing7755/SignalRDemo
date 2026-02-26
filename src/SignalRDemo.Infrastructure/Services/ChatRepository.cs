using SignalRDemo.Shared.Models;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 消息仓储实现
/// </summary>
public class ChatRepository : IChatRepository
{
    private readonly List<ChatMessage> _messages = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private const int MaxMessages = 1000;
    private const int MaxRoomMessages = 500;

    public Task SaveMessageAsync(ChatMessage message)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Add(message);

            // 限制全局消息数量
            if (_messages.Count > MaxMessages)
            {
                var oldest = _messages.OrderBy(m => m.Timestamp).Take(_messages.Count - MaxMessages).ToList();
                foreach (var msg in oldest)
                {
                    _messages.Remove(msg);
                }
            }

            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task SaveRoomMessageAsync(ChatMessage message)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Add(message);

            // 限制每个房间的消息数量
            var roomMessages = _messages.Where(m => m.RoomId == message.RoomId).ToList();
            if (roomMessages.Count > MaxRoomMessages)
            {
                var oldest = roomMessages
                    .OrderBy(m => m.Timestamp)
                    .Take(roomMessages.Count - MaxRoomMessages)
                    .ToList();

                foreach (var msg in oldest)
                {
                    _messages.Remove(msg);
                }
            }

            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<List<ChatMessage>> GetRecentMessagesAsync(int count = 50)
    {
        _lock.EnterReadLock();
        try
        {
            var messages = _messages
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToList();

            return Task.FromResult(messages);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<ChatMessage>> GetRoomMessagesAsync(string roomId, int count = 50)
    {
        _lock.EnterReadLock();
        try
        {
            var messages = _messages
                .Where(m => m.RoomId == roomId)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToList();

            return Task.FromResult(messages);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
