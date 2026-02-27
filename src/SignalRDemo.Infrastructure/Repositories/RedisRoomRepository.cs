using System.Text.Json;
using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;
using StackExchange.Redis;

namespace SignalRDemo.Infrastructure.Repositories;

/// <summary>
/// Redis 房间仓储实现
/// </summary>
public class RedisRoomRepository : IRoomRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private const string RoomKeyPrefix = "room:";
    private const string RoomNameIndexKey = "rooms:index:name";
    private const string PublicRoomsKey = "rooms:public";
    private const string RoomMembersKeyPrefix = "room:members:";

    public RedisRoomRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ChatRoom?> GetByIdAsync(RoomId id, CancellationToken cancellationToken = default)
    {
        var key = $"{RoomKeyPrefix}{id.Value}";
        var value = await _db.StringGetAsync(key);
        
        if (value.IsNull)
            return null;
        
        return DeserializeRoom(value!);
    }

    public async Task<ChatRoom?> GetByNameAsync(RoomName name, CancellationToken cancellationToken = default)
    {
        var roomId = await _db.HashGetAsync(RoomNameIndexKey, name.Value.ToLowerInvariant());
        
        if (roomId.IsNull)
            return null;
        
        return await GetByIdAsync(RoomId.Create(roomId!), cancellationToken);
    }

    public async Task<List<ChatRoom>> GetPublicRoomsAsync(CancellationToken cancellationToken = default)
    {
        var roomIds = await _db.SetMembersAsync(PublicRoomsKey);
        var rooms = new List<ChatRoom>();
        
        foreach (var roomId in roomIds)
        {
            var room = await GetByIdAsync(RoomId.Create(roomId!), cancellationToken);
            if (room != null)
            {
                rooms.Add(room);
            }
        }
        
        return rooms
            .OrderByDescending(r => r.MemberCount)
            .ThenBy(r => r.Name.Value)
            .ToList();
    }

    public async Task<List<ChatRoom>> GetUserRoomsAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        // 扫描所有房间找出用户所在的房间
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{RoomKeyPrefix}*").ToArray();
        
        var rooms = new List<ChatRoom>();
        foreach (var key in keys)
        {
            var value = await _db.StringGetAsync(key);
            if (!value.IsNull)
            {
                var room = DeserializeRoom(value!);
                if (room != null && room.ContainsMember(userId))
                {
                    rooms.Add(room);
                }
            }
        }
        
        return rooms.OrderBy(r => r.Name.Value).ToList();
    }

    public async Task<List<ChatRoom>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var term = searchTerm.ToLowerInvariant();
        
        // 使用 SCAN 搜索
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{RoomKeyPrefix}*").ToArray();
        
        var rooms = new List<ChatRoom>();
        foreach (var key in keys)
        {
            var value = await _db.StringGetAsync(key);
            if (!value.IsNull)
            {
                var room = DeserializeRoom(value!);
                if (room != null && room.Name.Value.ToLowerInvariant().Contains(term))
                {
                    rooms.Add(room);
                }
            }
        }
        
        return rooms
            .OrderByDescending(r => r.MemberCount)
            .ThenBy(r => r.Name.Value)
            .ToList();
    }

    public async Task<ChatRoom> AddAsync(ChatRoom room, CancellationToken cancellationToken = default)
    {
        var key = $"{RoomKeyPrefix}{room.Id.Value}";
        var json = JsonSerializer.Serialize(room, _jsonOptions);
        
        // 存储房间数据
        await _db.StringSetAsync(key, json);
        
        // 建立房间名索引
        await _db.HashSetAsync(RoomNameIndexKey, 
            room.Name.Value.ToLowerInvariant(), 
            room.Id.Value);
        
        // 如果是公开房间，加入公开房间集合
        if (room.IsPublic)
        {
            await _db.SetAddAsync(PublicRoomsKey, room.Id.Value);
        }
        
        // 存储成员列表
        if (room.MemberIds.Any())
        {
            var membersKey = $"{RoomMembersKeyPrefix}{room.Id.Value}";
            var memberValues = room.MemberIds.Select(m => (RedisValue)m.Value).ToArray();
            await _db.SetAddAsync(membersKey, memberValues);
        }
        
        return room;
    }

    public async Task UpdateAsync(ChatRoom room, CancellationToken cancellationToken = default)
    {
        var key = $"{RoomKeyPrefix}{room.Id.Value}";
        var json = JsonSerializer.Serialize(room, _jsonOptions);
        
        await _db.StringSetAsync(key, json);
        
        // 更新公开房间集合
        if (room.IsPublic)
        {
            await _db.SetAddAsync(PublicRoomsKey, room.Id.Value);
        }
        else
        {
            await _db.SetRemoveAsync(PublicRoomsKey, room.Id.Value);
        }
        
        // 更新成员列表
        var membersKey = $"{RoomMembersKeyPrefix}{room.Id.Value}";
        await _db.KeyDeleteAsync(membersKey);
        if (room.MemberIds.Any())
        {
            var memberValues = room.MemberIds.Select(m => (RedisValue)m.Value).ToArray();
            await _db.SetAddAsync(membersKey, memberValues);
        }
    }

    public async Task DeleteAsync(RoomId id, CancellationToken cancellationToken = default)
    {
        var key = $"{RoomKeyPrefix}{id.Value}";
        
        // 获取房间数据用于清理索引
        var value = await _db.StringGetAsync(key);
        if (!value.IsNull)
        {
            var room = DeserializeRoom(value!);
            if (room != null)
            {
                // 删除房间名索引
                await _db.HashDeleteAsync(RoomNameIndexKey, 
                    room.Name.Value.ToLowerInvariant());
                
                // 从公开房间集合中移除
                await _db.SetRemoveAsync(PublicRoomsKey, id.Value);
            }
        }
        
        // 删除房间数据
        await _db.KeyDeleteAsync(key);
        
        // 删除成员列表
        var membersKey = $"{RoomMembersKeyPrefix}{id.Value}";
        await _db.KeyDeleteAsync(membersKey);
    }

    public async Task EnsureLobbyExistsAsync(CancellationToken cancellationToken = default)
    {
        var lobby = await GetByIdAsync(RoomId.From("lobby"), cancellationToken);
        if (lobby == null)
        {
            var newLobby = ChatRoom.CreateLobby();
            await AddAsync(newLobby, cancellationToken);
        }
    }

    private ChatRoom? DeserializeRoom(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var id = root.GetProperty("id").GetProperty("value").GetString();
            var name = root.GetProperty("name").GetProperty("value").GetString();
            var description = root.TryGetProperty("description", out var desc) 
                ? desc.GetString() 
                : null;
            var ownerId = root.GetProperty("ownerId").GetProperty("value").GetString();
            var isPublic = root.GetProperty("isPublic").GetBoolean();
            var createdAt = root.GetProperty("createdAt").GetDateTime();
            var memberCount = root.GetProperty("memberCount").GetInt32();
            var version = root.TryGetProperty("version", out var v) ? v.GetInt32() : 1;
            
            HashedPassword? password = null;
            if (root.TryGetProperty("password", out var pwd) && pwd.ValueKind != JsonValueKind.Null)
            {
                var pwdValue = pwd.GetProperty("value").GetString();
                if (pwdValue != null)
                {
                    password = HashedPassword.From(pwdValue);
                }
            }
            
            List<UserId>? memberIds = null;
            if (root.TryGetProperty("memberIds", out var mids))
            {
                memberIds = new List<UserId>();
                foreach (var mid in mids.EnumerateArray())
                {
                    var midValue = mid.GetProperty("value").GetString();
                    if (midValue != null)
                    {
                        memberIds.Add(UserId.Create(midValue));
                    }
                }
            }
            
            var room = ChatRoom.Reconstitute(
                RoomId.Create(id!),
                RoomName.Create(name!),
                description,
                UserId.Create(ownerId!),
                isPublic,
                password,
                createdAt,
                memberCount,
                memberIds,
                version
            );
            
            return room;
        }
        catch
        {
            return null;
        }
    }
}
