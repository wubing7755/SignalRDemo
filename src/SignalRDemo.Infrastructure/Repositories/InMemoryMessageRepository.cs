using SignalRDemo.Domain.Entities;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Infrastructure.Repositories;

public class InMemoryMessageRepository : IMessageRepository
{
    private readonly List<ChatMessage> _messages = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private const int MaxMessagesPerRoom = 500;

    public Task<ChatMessage?> GetByIdAsync(MessageId id, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var message = _messages.FirstOrDefault(m => m.Id.Value == id.Value);
            return Task.FromResult(message);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<ChatMessage>> GetRoomMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var messages = _messages
                .Where(m => m.RoomId.Value == roomId.Value)
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

    public Task<int> GetRoomMessageCountAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var count = _messages.Count(m => m.RoomId.Value == roomId.Value);
            return Task.FromResult(count);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Add(message);

            // 限制每个房间的最大消息数
            var roomMessages = _messages.Where(m => m.RoomId.Value == message.RoomId.Value).ToList();
            if (roomMessages.Count > MaxMessagesPerRoom)
            {
                var oldestMessages = roomMessages
                    .OrderBy(m => m.Timestamp)
                    .Take(roomMessages.Count - MaxMessagesPerRoom)
                    .ToList();

                foreach (var msg in oldestMessages)
                {
                    _messages.Remove(msg);
                }
            }

            return Task.FromResult(message);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task DeleteAsync(MessageId id, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.RemoveAll(m => m.Id.Value == id.Value);
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
