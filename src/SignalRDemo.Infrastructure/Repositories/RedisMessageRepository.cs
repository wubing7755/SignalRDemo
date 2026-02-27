using System.Text.Json;
using SignalRDemo.Domain.Entities;
using SignalRDemo.Domain.Repositories;
using SignalRDemo.Domain.ValueObjects;
using StackExchange.Redis;

namespace SignalRDemo.Infrastructure.Repositories;

/// <summary>
/// Redis 消息仓储实现
/// - Redis List 用于实时消息队列
/// - 文件系统用于历史消息持久化
/// </summary>
public class RedisMessageRepository : IMessageRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private const string MessageKeyPrefix = "message:";
    private const string RoomMessagesKeyPrefix = "room:messages:";
    private const string MessageIndexKey = "messages:index";
    
    private readonly string _storagePath;
    private const int MaxCachedMessages = 500;

    public RedisMessageRepository(IConnectionMultiplexer redis, string storagePath = "messages")
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _storagePath = storagePath;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        // 确保存储目录存在
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<ChatMessage?> GetByIdAsync(MessageId id, CancellationToken cancellationToken = default)
    {
        var key = $"{MessageKeyPrefix}{id.Value}";
        var value = await _db.StringGetAsync(key);
        
        if (value.IsNull)
        {
            // 从文件加载
            return await LoadFromFileAsync(id);
        }
        
        return DeserializeMessage(value!);
    }

    public async Task<List<ChatMessage>> GetRoomMessagesAsync(RoomId roomId, int count = 50, CancellationToken cancellationToken = default)
    {
        var messagesKey = $"{RoomMessagesKeyPrefix}{roomId.Value}";
        var messageIds = await _db.ListRangeAsync(messagesKey, 0, count - 1);
        
        var messages = new List<ChatMessage>();
        foreach (var msgId in messageIds.Reverse())
        {
            var message = await GetByIdAsync(MessageId.Create(msgId!), cancellationToken);
            if (message != null)
            {
                messages.Add(message);
            }
        }
        
        return messages;
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        // 获取所有房间的最新消息
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{RoomMessagesKeyPrefix}*").ToArray();
        
        var allMessages = new List<ChatMessage>();
        
        foreach (var key in keys)
        {
            var messageIds = await _db.ListRangeAsync(key, 0, 9); // 每个房间取10条
            foreach (var msgId in messageIds)
            {
                var message = await GetByIdAsync(MessageId.Create(msgId!), cancellationToken);
                if (message != null)
                {
                    allMessages.Add(message);
                }
            }
        }
        
        return allMessages
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .ToList();
    }

    public async Task<int> GetRoomMessageCountAsync(RoomId roomId, CancellationToken cancellationToken = default)
    {
        var messagesKey = $"{RoomMessagesKeyPrefix}{roomId.Value}";
        return (int)await _db.ListLengthAsync(messagesKey);
    }

    public async Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        var key = $"{MessageKeyPrefix}{message.Id.Value}";
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        
        // 存储到 Redis
        await _db.StringSetAsync(key, json);
        
        // 添加到房间消息列表
        var messagesKey = $"{RoomMessagesKeyPrefix}{message.RoomId.Value}";
        await _db.ListLeftPushAsync(messagesKey, message.Id.Value);
        
        // 限制房间消息数量
        await TrimRoomMessagesAsync(message.RoomId.Value);
        
        // 添加到全局索引
        await _db.HashSetAsync(MessageIndexKey, message.Id.Value, message.RoomId.Value);
        
        // 持久化到文件（异步）
        await SaveToFileAsync(message);
        
        return message;
    }

    public async Task DeleteAsync(MessageId id, CancellationToken cancellationToken = default)
    {
        var key = $"{MessageKeyPrefix}{id.Value}";
        
        // 获取消息的房间ID
        var roomId = await _db.HashGetAsync(MessageIndexKey, id.Value);
        
        // 从 Redis 删除
        await _db.KeyDeleteAsync(key);
        
        // 从房间消息列表中移除
        if (!roomId.IsNull)
        {
            var messagesKey = $"{RoomMessagesKeyPrefix}{roomId}";
            await _db.ListRemoveAsync(messagesKey, id.Value, 0);
        }
        
        // 从全局索引中删除
        await _db.HashDeleteAsync(MessageIndexKey, id.Value);
        
        // 删除文件
        await DeleteFromFileAsync(id);
    }

    private async Task TrimRoomMessagesAsync(string roomId)
    {
        var messagesKey = $"{RoomMessagesKeyPrefix}{roomId}";
        var length = await _db.ListLengthAsync(messagesKey);
        
        if (length > MaxCachedMessages)
        {
            // 保留最新的 MaxCachedMessages 条
            await _db.ListTrimAsync(messagesKey, 0, MaxCachedMessages - 1);
        }
    }

    private async Task SaveToFileAsync(ChatMessage message)
    {
        try
        {
            var date = message.Timestamp.ToString("yyyy-MM-dd");
            var roomFilePath = Path.Combine(_storagePath, "by-room", message.RoomId.Value, $"{date}.json");
            var dateFilePath = Path.Combine(_storagePath, "by-date", $"{date}.json");
            
            // 保存到按房间的目录
            var roomDir = Path.GetDirectoryName(roomFilePath);
            if (roomDir != null)
            {
                Directory.CreateDirectory(roomDir);
            }
            
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            
            // 追加到房间文件
            await AppendToFileAsync(roomFilePath, json);
            
            // 追加到日期文件
            var dateDir = Path.GetDirectoryName(dateFilePath);
            if (dateDir != null)
            {
                Directory.CreateDirectory(dateDir);
            }
            await AppendToFileAsync(dateFilePath, json);
        }
        catch
        {
            // 忽略文件保存错误
        }
    }

    private async Task AppendToFileAsync(string filePath, string json)
    {
        using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(stream);
        await writer.WriteLineAsync(json);
    }

    private async Task<ChatMessage?> LoadFromFileAsync(MessageId id)
    {
        // 尝试从多个日期文件中查找
        var today = DateTime.UtcNow;
        
        for (int i = 0; i < 7; i++) // 搜索最近7天
        {
            var date = today.AddDays(-i).ToString("yyyy-MM-dd");
            
            // 搜索所有房间目录
            var roomDir = Path.Combine(_storagePath, "by-room");
            if (!Directory.Exists(roomDir))
                continue;
            
            foreach (var roomPath in Directory.GetDirectories(roomDir))
            {
                var filePath = Path.Combine(roomPath, $"{date}.json");
                if (!File.Exists(filePath))
                    continue;
                
                var message = await SearchInFileAsync(filePath, id);
                if (message != null)
                    return message;
            }
        }
        
        return null;
    }

    private async Task<ChatMessage?> SearchInFileAsync(string filePath, MessageId id)
    {
        try
        {
            if (!File.Exists(filePath))
                return null;
            
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.Contains(id.Value))
                {
                    return DeserializeMessage(line);
                }
            }
        }
        catch
        {
            // 忽略读取错误
        }
        
        return null;
    }

    private async Task DeleteFromFileAsync(MessageId id)
    {
        // 文件系统删除比较复杂，这里只标记或跳过
        await Task.CompletedTask;
    }

    private ChatMessage? DeserializeMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            // 获取各种属性
            var id = root.GetProperty("id").GetProperty("value").GetString();
            var userId = root.GetProperty("userId").GetProperty("value").GetString();
            var userName = root.GetProperty("userName").GetProperty("value").GetString();
            var displayName = root.TryGetProperty("displayName", out var dn) 
                ? dn.GetProperty("value").GetString() 
                : userName;
            var roomId = root.GetProperty("roomId").GetProperty("value").GetString();
            var content = root.GetProperty("content").GetString();
            var timestamp = root.GetProperty("timestamp").GetDateTime();
            var messageType = root.TryGetProperty("messageType", out var mt) ? mt.GetString() : "Text";
            var mediaUrl = root.TryGetProperty("mediaUrl", out var mu) ? mu.GetString() : null;
            var altText = root.TryGetProperty("altText", out var at) ? at.GetString() : null;
            
            return ChatMessage.Reconstitute(
                MessageId.Create(id!),
                UserId.Create(userId!),
                UserName.Create(userName!),
                DisplayName.Create(displayName ?? userName!),
                RoomId.Create(roomId!),
                content!,
                messageType ?? "Text",
                mediaUrl,
                altText,
                timestamp
            );
        }
        catch
        {
            return null;
        }
    }
}
