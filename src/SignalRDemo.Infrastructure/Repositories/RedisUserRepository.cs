using System.Text.Json;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<RedisUserRepository> _logger;
    
    private const string UserKeyPrefix = "user:";
    private const string UserNameIndexKey = "users:index:username";

    public RedisUserRepository(IConnectionMultiplexer redis, ILogger<RedisUserRepository> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
    {
        var key = $"{UserKeyPrefix}{id.Value}";
        var value = await _db.StringGetAsync(key);
        
        if (value.IsNull)
        {
            _logger.LogDebug("用户不存在: {UserId}", id.Value);
            return null;
        }
        
        try
        {
            return DeserializeUser(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "反序列化用户失败: {UserId}, JSON: {Json}", id.Value, value);
            return null;
        }
    }

    public async Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default)
    {
        var userId = await _db.HashGetAsync(UserNameIndexKey, userName.Value.ToLowerInvariant());
        
        if (userId.IsNull)
        {
            _logger.LogDebug("用户名不存在: {UserName}", userName.Value);
            return null;
        }
        
        _logger.LogDebug("找到用户名索引: {UserName} -> {UserId}", userName.Value, userId);
        
        return await GetByIdAsync(UserId.Create(userId!), cancellationToken);
    }

    public async Task<bool> ExistsAsync(UserName userName, CancellationToken cancellationToken = default)
    {
        var exists = await _db.HashExistsAsync(UserNameIndexKey, userName.Value.ToLowerInvariant());
        _logger.LogDebug("检查用户是否存在: {UserName}, 结果: {Exists}", userName.Value, exists);
        return exists;
    }

    public async Task<User> AddAsync(User user, CancellationToken cancellationToken = default)
    {
        var key = $"{UserKeyPrefix}{user.Id.Value}";
        
        // 调试：打印用户信息
        _logger.LogDebug("添加用户: Id={UserId}, UserName={UserName}, PasswordHash={PasswordHash}", 
            user.Id.Value, 
            user.UserName.Value,
            user.PasswordHash.Value);
        
        // 手动构建 JSON 以确保正确的格式
        var json = BuildUserJson(user);
        
        // 存储用户数据
        await _db.StringSetAsync(key, json);
        
        // 建立用户名索引
        await _db.HashSetAsync(UserNameIndexKey, 
            user.UserName.Value.ToLowerInvariant(), 
            user.Id.Value);
        
        _logger.LogInformation("用户注册成功: {UserName}", user.UserName.Value);
        
        return user;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var key = $"{UserKeyPrefix}{user.Id.Value}";
        var json = BuildUserJson(user);
        
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

    private string BuildUserJson(User user)
    {
        // 手动构建 JSON，确保属性名正确
        return JsonSerializer.Serialize(new
        {
            id = new { value = user.Id.Value },
            userName = new { value = user.UserName.Value },
            displayName = new { value = user.DisplayName.Value },
            passwordHash = new { value = user.PasswordHash.Value },
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt,
            version = user.Version
        });
    }

    private User? DeserializeUser(string json)
    {
        _logger.LogDebug("开始反序列化用户 JSON: {Json}", json);
        
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        
        // 读取 id
        var idElement = root.GetProperty("id");
        var id = idElement.GetProperty("value").GetString();
        
        // 读取 userName
        var userNameElement = root.GetProperty("userName");
        var userName = userNameElement.GetProperty("value").GetString();
        
        // 读取 displayName
        string? displayName = null;
        if (root.TryGetProperty("displayName", out var dnElement) && dnElement.ValueKind != JsonValueKind.Null)
        {
            displayName = dnElement.GetProperty("value").GetString();
        }
        
        // 读取 passwordHash
        var passwordHashElement = root.GetProperty("passwordHash");
        var passwordHashValue = passwordHashElement.GetProperty("value").GetString();
        
        // 读取 createdAt
        var createdAt = root.GetProperty("createdAt").GetDateTime();
        
        // 读取 lastLoginAt
        DateTime? lastLoginAt = null;
        if (root.TryGetProperty("lastLoginAt", out var llElement) && llElement.ValueKind != JsonValueKind.Null)
        {
            lastLoginAt = llElement.GetDateTime();
        }
        
        // 读取 version
        int version = 1;
        if (root.TryGetProperty("version", out var vElement))
        {
            version = vElement.GetInt32();
        }
        
        _logger.LogDebug("反序列化成功: id={Id}, userName={UserName}, passwordHash={PasswordHash}", 
            id, userName, passwordHashValue);
        
        // 构建 User 对象
        var hashedPassword = HashedPassword.From(passwordHashValue!);
        
        var user = User.Reconstitute(
            UserId.Create(id!),
            UserName.Create(userName!),
            DisplayName.Create(displayName ?? userName!),
            hashedPassword,
            createdAt,
            lastLoginAt,
            version
        );
        
        return user;
    }
}
