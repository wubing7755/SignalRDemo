using System.Security.Cryptography;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 房间服务实现
/// </summary>
public class RoomService : IRoomService
{
    private readonly List<ChatRoom> _rooms = new();
    private readonly List<UserRoom> _userRooms = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<RoomService> _logger;

    public RoomService(ILogger<RoomService> logger)
    {
        _logger = logger;
        // 创建默认大厅
        CreateDefaultLobby();
    }

    private void CreateDefaultLobby()
    {
        var lobby = new ChatRoom
        {
            Id = "lobby",
            Name = "大厅",
            Description = "公共聊天大厅，欢迎所有人加入",
            OwnerId = "system",
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            MemberCount = 0
        };
        _rooms.Add(lobby);
    }

    public Task<ChatRoom> CreateRoomAsync(string name, string? description, string ownerId, bool isPublic, string? password)
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
                Password = isPublic ? null : HashPassword(password ?? string.Empty),
                CreatedAt = DateTime.UtcNow,
                MemberCount = 1
            };

            _rooms.Add(room);

            // 创建者自动加入房间
            _userRooms.Add(new UserRoom
            {
                UserId = ownerId,
                RoomId = room.Id,
                JoinedAt = DateTime.UtcNow,
                Role = RoomRole.Owner
            });

            _logger.LogInformation("房间创建成功: {RoomName} (ID: {RoomId})", name, room.Id);

            return Task.FromResult(room);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<List<ChatRoom>> GetPublicRoomsAsync()
    {
        _lock.EnterReadLock();
        try
        {
            var rooms = _rooms
                .Where(r => r.IsPublic)
                .OrderByDescending(r => r.MemberCount)
                .ThenBy(r => r.Name)
                .ToList();

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
            var userRoomIds = _userRooms
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoomId)
                .ToHashSet();

            var rooms = _rooms
                .Where(r => userRoomIds.Contains(r.Id))
                .OrderBy(r => r.Name)
                .ToList();

            return Task.FromResult(rooms);
        }
        finally
        {
            _lock.ExitReadLock();
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

    public Task<bool> VerifyPasswordAsync(string roomId, string password)
    {
        _lock.EnterReadLock();
        try
        {
            var room = _rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null || room.IsPublic)
            {
                return Task.FromResult(false);
            }

            if (string.IsNullOrEmpty(room.Password))
            {
                return Task.FromResult(true);
            }

            var result = VerifyPassword(password, room.Password);
            return Task.FromResult(result);
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
            var room = _rooms.FirstOrDefault(r => r.Id == roomId);
            if (room == null)
            {
                return Task.FromResult(false);
            }

            // 检查是否已在房间中
            var existing = _userRooms.FirstOrDefault(ur => 
                ur.UserId == userId && ur.RoomId == roomId);
            if (existing != null)
            {
                return Task.FromResult(true);
            }

            _userRooms.Add(new UserRoom
            {
                UserId = userId,
                RoomId = roomId,
                JoinedAt = DateTime.UtcNow,
                Role = RoomRole.Member
            });

            room.MemberCount++;

            _logger.LogInformation("用户 {UserId} 加入房间 {RoomId}", userId, roomId);
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
            var userRoom = _userRooms.FirstOrDefault(ur => 
                ur.UserId == userId && ur.RoomId == roomId);
            if (userRoom == null)
            {
                return Task.FromResult(false);
            }

            _userRooms.Remove(userRoom);

            var room = _rooms.FirstOrDefault(r => r.Id == roomId);
            if (room != null && room.MemberCount > 0)
            {
                room.MemberCount--;
            }

            _logger.LogInformation("用户 {UserId} 离开房间 {RoomId}", userId, roomId);
            return Task.FromResult(true);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<int> GetRoomMemberCountAsync(string roomId)
    {
        _lock.EnterReadLock();
        try
        {
            var count = _userRooms.Count(ur => ur.RoomId == roomId);
            return Task.FromResult(count);
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
            var exists = _userRooms.Any(ur => ur.UserId == userId && ur.RoomId == roomId);
            return Task.FromResult(exists);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 使用 PBKDF2 哈希密码
    /// </summary>
    private static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var saltHex = Convert.ToHexString(salt);
        var hashHex = Convert.ToHexString(hash);

        return $"{saltHex}:{hashHex}";
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromHexString(parts[0]);
            var storedHashBytes = Convert.FromHexString(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
        }
        catch
        {
            return false;
        }
    }
}
