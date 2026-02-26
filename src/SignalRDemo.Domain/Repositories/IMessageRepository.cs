using SignalRDemo.Domain.Entities;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Domain.Repositories;

public interface IMessageRepository
{
    Task<ChatMessage?> GetByIdAsync(MessageId id, CancellationToken cancellationToken = default);
    Task<List<ChatMessage>> GetRoomMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default);
    Task<int> GetRoomMessageCountAsync(RoomId roomId, CancellationToken cancellationToken = default);
    Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task DeleteAsync(MessageId id, CancellationToken cancellationToken = default);
}
