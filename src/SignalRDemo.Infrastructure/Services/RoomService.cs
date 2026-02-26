using SignalRDemo.Shared.Models;
using System.Security.Cryptography;
using System.Text;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 房间服务实现
/// </summary>
public class RoomService : IRoomService
{
    private readonly List<ChatRoom> _rooms = new();
    private readonly Dictionary<string, List<string>> _roomUsers = new(); // roomId -> userIds
    private readonly ReaderWriterLockSlim _lock = new();

    public RoomService()
    {
        // 创建默认大厅
        _rooms.Add(new ChatRoom
        {
            Id = "lobby",
            Name = "大厅",
            Description = "公共聊天室",
            OwnerId = "system",
            IsPublic = true,
            MemberCount = 0,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task<ChatRoom?> CreateRoomAsync(string name, string? description, string ownerId, bool isPublic, string? password)
    {
        _lock.EnterWriteLock();
        try
        {
            var room = new ChatRoom
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                OwnerId = ownerId,
                IsPublic = isPublic,
                Password = isPublic ? null : HashPassword(password ?? ""),
                MemberCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _rooms.Add(room);
            _roomUsers[room.Id] = new List<string>();
            
            return Task.FromResult<ChatRoom?>(room);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<ChatRoom?> GetRoomByIdAsync(string roomId)
    {
        _lock.EnterReadLock();
        try
        {
            var room = _rooms.FirstOrDefault(r => r.Id == roomId);
            return Task.FromResult(room);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<ChatRoom?> GetRoomByNameAsync(string name)
    {
        _lock.EnterReadLock();
        try
        {
            var room = _rooms.FirstOrDefault(r => 
                r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(room);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<ChatRoom>> GetPublicRoomsAsync()
    {
        _lock.EnterReadLock();
        try
        {
            var rooms = _rooms.Where(r => r.IsPublic).ToList();
            return Task.FromResult(rooms);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<ChatRoom>> GetUserRoomsAsync(string userId)
    {
        _lock.EnterReadLock();
        try
        {
            var roomIds = _roomUsers.Where(kv => kv.Value.Contains(userId)).Select(kv => kv.Key).ToList();
            var rooms = _rooms.Where(r => roomIds.Contains(r.Id) || r.Id == "lobby").ToList();
            return Task.FromResult(rooms);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<ChatRoom>> FindRoomsByNameAsync(string name)
    {
        _lock.EnterReadLock();
        try
        {
            var rooms = _rooms.Where(r => 
                r.IsPublic && r.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult(rooms);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<bool> AddUserToRoomAsync(string userId, string roomId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_roomUsers.ContainsKey(roomId))
                _roomUsers[roomId] = new List<string>();

            if (!_roomUsers[roomId].Contains(userId))
            {
                _roomUsers[roomId].Add(userId);
                
                var room = _rooms.FirstOrDefault(r => r.Id == roomId);
                if (room != null)
                    room.MemberCount = _roomUsers[roomId].Count;
            }

            return Task.FromResult(true);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<bool> RemoveUserFromRoomAsync(string userId, string roomId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_roomUsers.ContainsKey(roomId))
            {
                _roomUsers[roomId].Remove(userId);
                
                var room = _rooms.FirstOrDefault(r => r.Id == roomId);
                if (room != null)
                    room.MemberCount = _roomUsers[roomId].Count;
            }

            return Task.FromResult(true);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<List<string>> GetRoomUserIdsAsync(string roomId)
    {
        _lock.EnterReadLock();
        try
        {
            var userIds = _roomUsers.ContainsKey(roomId) 
                ? _roomUsers[roomId].ToList() 
                : new List<string>();
            return Task.FromResult(userIds);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<bool> IsUserInRoomAsync(string userId, string roomId)
    {
        _lock.EnterReadLock();
        try
        {
            var isInRoom = _roomUsers.ContainsKey(roomId) && _roomUsers[roomId].Contains(userId);
            return Task.FromResult(isInRoom);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<bool> VerifyPasswordAsync(string roomId, string password)
    {
        _lock.EnterReadLock();
        try
        {
            var room = _rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null || room.IsPublic)
                return Task.FromResult(false);

            return Task.FromResult(room.Password == HashPassword(password));
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
