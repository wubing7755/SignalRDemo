using System.Security.Cryptography;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 基于 JSON 文件的房间服务实现
/// </summary>
public class JsonRoomService : IRoomService
{
    private readonly string _dataPath;
    private readonly string _filePath;
    private List<ChatRoom> _rooms = new();
    private List<UserRoom> _userRooms = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<JsonRoomService> _logger;
    private readonly object _saveLock = new();

    public JsonRoomService(ILogger<JsonRoomService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _dataPath = Path.Combine(env.ContentRootPath, "Data");
        _filePath = Path.Combine(_dataPath, "rooms.json");
        
        EnsureDataDirectory();
        LoadRooms();
    }

    private void EnsureDataDirectory()
    {
        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
            _logger.LogInformation("创建数据目录: {Path}", _dataPath);
        }
    }

    private void LoadRooms()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<RoomData>(json);
                if (data != null)
                {
                    _rooms = data.Rooms ?? new List<ChatRoom>();
                    _userRooms = data.UserRooms ?? new List<UserRoom>();
                    _logger.LogInformation("已加载 {RoomCount} 个房间, {UserRoomCount} 个用户房间关系", _rooms.Count, _userRooms.Count);
                }
            }
            else
            {
                _logger.LogInformation("房间数据文件不存在，将创建新文件");
            }
            
            // 确保大厅存在
            EnsureLobbyExists();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载房间数据失败");
            _rooms = new List<ChatRoom>();
            _userRooms = new List<UserRoom>();
            EnsureLobbyExists();
        }
    }

    private void EnsureLobbyExists()
    {
        if (!_rooms.Any(r => r.Id == "lobby"))
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
            SaveRooms();
        }
    }

    private void SaveRooms()
    {
        lock (_saveLock)
        {
            try
            {
                var data = new RoomData
                {
                    Rooms = _rooms,
                    UserRooms = _userRooms
                };
                var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存房间数据失败");
            }
        }
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

            SaveRooms();
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
            SaveRooms();

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
                SaveRooms();
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

    public Task<List<ChatRoom>> FindRoomsByNameAsync(string roomName)
    {
        _lock.EnterReadLock();
        try
        {
            var searchTerm = roomName.ToLowerInvariant();
            var rooms = _rooms
                .Where(r => r.Name.ToLowerInvariant().Contains(searchTerm))
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

    public Task<ChatRoom?> GetRoomByNameAsync(string roomName)
    {
        _lock.EnterReadLock();
        try
        {
            var room = _rooms.FirstOrDefault(r => 
                r.Name.Equals(roomName, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(room);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<List<string>> GetRoomUserIdsAsync(string roomId)
    {
        _lock.EnterReadLock();
        try
        {
            var userIds = _userRooms
                .Where(ur => ur.RoomId == roomId)
                .Select(ur => ur.UserId)
                .ToList();
            return Task.FromResult(userIds);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 房间数据存储模型
    /// </summary>
    private class RoomData
    {
        public List<ChatRoom> Rooms { get; set; } = new();
        public List<UserRoom> UserRooms { get; set; } = new();
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
