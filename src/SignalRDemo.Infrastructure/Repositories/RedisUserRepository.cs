using System.Text.Json;
using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;
using StackExchange.Redis;

namespace SignalRDemo.Infrastructure.Repositories;

/// <summary>
/// Redis 用户仓储实现
/// </summary>
public class RedisUserRepository : IUserRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private const string UserKeyPrefix = "user:";
    private const string UserNameIndexKey = "users:index:username";

    public RedisUserRepository(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var key = $"{UserKeyPrefix}{id.Value}";
        var value = await _db.StringGetAsync(key);
        
        if (value.IsNull)
            return null;
        
        return DeserializeUser(value!);
    }

    public async Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default)
    {
        var userId = await _db.HashGetAsync(UserNameIndexKey, userName.Value.ToLowerInvariant());
        
        if (userId.IsNull)
            return null;
        
        return await GetByIdAsync(UserId.Create(userId!), cancellationToken);
    }

    public async Task<bool> ExistsAsync(UserName userName, CancellationToken cancellationToken = default)
    {
        var exists = await _db.HashExistsAsync(UserNameIndexKey, userName.Value.ToLowerInvariant());
        return exists;
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var key = $"{UserKeyPrefix}{user.Id.Value}";
        var json = JsonSerializer.Serialize(user, _jsonOptions);
        
        // 存储用户数据
        await _db.StringSetAsync(key, json);
        
        // 建立用户名索引
        await _db.HashSetAsync(UserNameIndexKey, 
            user.UserName.Value.ToLowerInvariant(), 
            user.Id.Value);
        
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var key = $"{UserKeyPrefix}{user.Id.Value}";
        var json = JsonSerializer.Serialize(user, _jsonOptions);
        
        await _db.StringSetAsync(key, json);
    }

    public async Task DeleteAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var key = $"{UserKeyPrefix}{id.Value}";
        
        // 获取用户名用于删除索引
        var value = await _db.StringGetAsync(key);
        if (!value.IsNull)
        {
            var user = DeserializeUser(value!);
            if (user != null)
            {
                // 删除用户名索引
                await _db.HashDeleteAsync(UserNameIndexKey, 
                    user.UserName.Value.ToLowerInvariant());
            }
        }
        
        // 删除用户数据
        await _db.KeyDeleteAsync(key);
    }

    private User? DeserializeUser(string json)
    {
        try
        {
            // 手动反序列化以处理 Domain Aggregate
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            var id = root.GetProperty("id").GetString();
            var userName = root.GetProperty("userName").GetProperty("value").GetString();
            var displayName = root.TryGetProperty("displayName", out var dn) 
                ? dn.GetProperty("value").GetString() 
                : null;
            var passwordHash = root.GetProperty("passwordHash").GetProperty("value").GetString();
            
            // 使用 HashedPassword.From 而不是 Create
            var hashedPassword = HashedPassword.From(passwordHash!);
            
            var user = User.Reconstitute(
                UserId.Create(id!),
                UserName.Create(userName!),
                DisplayName.Create(displayName ?? userName!),
                hashedPassword,
                root.GetProperty("createdAt").GetDateTime(),
                root.TryGetProperty("lastLoginAt", out var ll) && ll.ValueKind != JsonValueKind.Null 
                    ? ll.GetDateTime() 
                    : null,
                root.TryGetProperty("version", out var v) ? v.GetInt32() : 1
            );
            
            return user;
        }
        catch
        {
            return null;
        }
    }
}
