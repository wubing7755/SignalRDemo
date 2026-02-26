using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Domain.Repositories;

public interface IRoomRepository
{
    Task<ChatRoom?> GetByIdAsync(RoomId id, CancellationToken cancellationToken = default);
    Task<ChatRoom?> GetByNameAsync(RoomName name, CancellationToken cancellationToken = default);
    Task<List<ChatRoom>> GetPublicRoomsAsync(CancellationToken cancellationToken = default);
    Task<List<ChatRoom>> GetUserRoomsAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<List<ChatRoom>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<ChatRoom> AddAsync(ChatRoom room, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChatRoom room, CancellationToken cancellationToken = default);
    Task DeleteAsync(RoomId id, CancellationToken cancellationToken = default);
    Task EnsureLobbyExistsAsync(CancellationToken cancellationToken = default);
}
