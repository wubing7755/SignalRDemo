using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// Redis 消息缓存服务实现
/// 使用 Redis List 实现可靠的消息队列
/// </summary>
public class RedisMessageCacheService : IMessageCacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisMessageCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Redis 键常量
    private const string MessageQueueKey = "messages:queue:list";
    private const string OnlineUsersKey = "users:online";
    private const string MessagePrefix = "message:";
    private const int MessageCacheExpiryHours = 24;

    public RedisMessageCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisMessageCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task EnqueueAsync(MessageCacheDto message)
    {
        var db = _redis.GetDatabase();
        
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            
            // 使用 List 左侧入队
            await db.ListLeftPushAsync(MessageQueueKey, json);
            
            _logger.LogDebug("消息已加入 Redis 队列: {MessageId}", message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息入队失败: {MessageId}", message.Id);
            throw;
        }
    }

    public async Task<MessageCacheDto?> DequeueAsync(CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        
        try
        {
            // 从 List 右侧出队（先进先出）
            var json = await db.ListRightPopAsync(MessageQueueKey);
            
            if (json.IsNull)
            {
                return null;
            }
            
            var message = JsonSerializer.Deserialize<MessageCacheDto>(json!, _jsonOptions);
            
            _logger.LogDebug("消息已从队列取出: {MessageId}", message?.Id);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "消息出队失败");
            return null;
        }
    }

    public async Task<MessageCacheDto?> GetAsync(string messageId)
    {
        var db = _redis.GetDatabase();
        var key = $"{MessagePrefix}{messageId}";
        
        var value = await db.StringGetAsync(key);
        if (value.IsNull)
        {
            return null;
        }
        
        return JsonSerializer.Deserialize<MessageCacheDto>(value!, _jsonOptions);
    }

    public async Task SetAsync(string messageId, MessageCacheDto message, TimeSpan expiry)
    {
        var db = _redis.GetDatabase();
        var key = $"{MessagePrefix}{messageId}";
        
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        await db.StringSetAsync(key, json, expiry);
    }

    public async Task SetUserOnlineAsync(string userId)
    {
        var db = _redis.GetDatabase();
        await db.SetAddAsync(OnlineUsersKey, userId);
    }

    public async Task SetUserOfflineAsync(string userId)
    {
        var db = _redis.GetDatabase();
        await db.SetRemoveAsync(OnlineUsersKey, userId);
    }

    public async Task<List<string>> GetOnlineUsersAsync()
    {
        var db = _redis.GetDatabase();
        var members = await db.SetMembersAsync(OnlineUsersKey);
        
        return members.Select(m => m.ToString()).ToList();
    }

    public async Task<bool> IsUserOnlineAsync(string userId)
    {
        var db = _redis.GetDatabase();
        return await db.SetContainsAsync(OnlineUsersKey, userId);
    }

    public async Task<long> GetQueueLengthAsync()
    {
        var db = _redis.GetDatabase();
        return await db.ListLengthAsync(MessageQueueKey);
    }
}
