using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;

namespace SignalRDemo.Infrastructure.Repositories;

public class InMemoryRoomRepository : IRoomRepository
{
    private readonly List<ChatRoom> _rooms = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public InMemoryRoomRepository()
    {
        // 确保大厅存在
        _lock.EnterWriteLock();
        try
        {
            if (!_rooms.Any(r => r.Id.Value == "lobby"))
            {
                _rooms.Add(ChatRoom.CreateLobby());
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<ChatRoom?> GetByIdAsync(RoomId id, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var room = _rooms.FirstOrDefault(r => r.Id.Value == id.Value);
            return Task.FromResult(room);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<ChatRoom?> GetByNameAsync(RoomName name, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var room = _rooms.FirstOrDefault(r => 
                r.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(room);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<ChatRoom>> GetPublicRoomsAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var rooms = _rooms
                .Where(r => r.IsPublic)
                .OrderByDescending(r => r.MemberCount)
                .ThenBy(r => r.Name.Value)
                .ToList();
            return Task.FromResult(rooms);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<ChatRoom>> GetUserRoomsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var rooms = _rooms
                .Where(r => r.ContainsMember(userId))
                .OrderBy(r => r.Name.Value)
                .ToList();
            return Task.FromResult(rooms);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<ChatRoom>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _lock.EnterReadLock();
        try
        {
            var term = searchTerm.ToLowerInvariant();
            var rooms = _rooms
                .Where(r => r.Name.Value.ToLowerInvariant().Contains(term))
                .OrderByDescending(r => r.MemberCount)
                .ThenBy(r => r.Name.Value)
                .ToList();
            return Task.FromResult(rooms);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<ChatRoom> AddAsync(ChatRoom room, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _rooms.Add(room);
            return Task.FromResult(room);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task UpdateAsync(ChatRoom room, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            var index = _rooms.FindIndex(r => r.Id.Value == room.Id.Value);
            if (index >= 0)
            {
                _rooms[index] = room;
            }
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task DeleteAsync(RoomId id, CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            _rooms.RemoveAll(r => r.Id.Value == id.Value);
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task EnsureLobbyExistsAsync(CancellationToken cancellationToken = default)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_rooms.Any(r => r.Id.Value == "lobby"))
            {
                _rooms.Add(ChatRoom.CreateLobby());
            }
            return Task.CompletedTask;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
