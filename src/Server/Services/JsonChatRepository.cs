using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 基于 JSON 文件的聊天消息仓储实现
/// </summary>
public class JsonChatRepository : IChatRepository
{
    private readonly string _dataPath;
    private readonly string _filePath;
    private List<ChatMessage> _messages = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<JsonChatRepository> _logger;
    private readonly object _saveLock = new();
    private readonly int _maxMessages = 1000;
    private readonly int _maxMessagesPerRoom = 500;

    public JsonChatRepository(ILogger<JsonChatRepository> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _dataPath = Path.Combine(env.ContentRootPath, "Data");
        _filePath = Path.Combine(_dataPath, "messages.json");
        
        EnsureDataDirectory();
        LoadMessages();
    }

    private void EnsureDataDirectory()
    {
        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
            _logger.LogInformation("创建数据目录: {Path}", _dataPath);
        }
    }

    private void LoadMessages()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var messages = System.Text.Json.JsonSerializer.Deserialize<List<ChatMessage>>(json);
                if (messages != null)
                {
                    _messages = messages;
                    _logger.LogInformation("已加载 {Count} 条消息", _messages.Count);
                }
            }
            else
            {
                _logger.LogInformation("消息数据文件不存在，将创建新文件");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载消息数据失败");
            _messages = new List<ChatMessage>();
        }
    }

    private void SaveMessages()
    {
        lock (_saveLock)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_messages, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存消息数据失败");
            }
        }
    }

    public Task SaveMessageAsync(ChatMessage message)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Add(message);
            
            // 限制最大消息数，防止内存无限增长
            if (_messages.Count > _maxMessages)
            {
                _messages.RemoveAt(0);
            }
            
            SaveMessages();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatMessage>> GetRecentMessagesAsync(int count = 50)
    {
        _lock.EnterReadLock();
        try
        {
            var result = _messages
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToList()
                .AsReadOnly();
            
            return Task.FromResult<IReadOnlyList<ChatMessage>>(result);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<int> GetMessageCountAsync()
    {
        _lock.EnterReadLock();
        try
        {
            return Task.FromResult(_messages.Count);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task SaveRoomMessageAsync(ChatMessage message)
    {
        _lock.EnterWriteLock();
        try
        {
            _messages.Add(message);
            
            // 限制每个房间的最大消息数
            var roomMessages = _messages.Where(m => m.RoomId == message.RoomId).ToList();
            if (roomMessages.Count > _maxMessagesPerRoom)
            {
                var oldestMessages = roomMessages
                    .OrderBy(m => m.Timestamp)
                    .Take(roomMessages.Count - _maxMessagesPerRoom)
                    .ToList();
                
                foreach (var msg in oldestMessages)
                {
                    _messages.Remove(msg);
                }
            }
            
            SaveMessages();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ChatMessage>> GetRoomMessagesAsync(string roomId, int count = 50)
    {
        _lock.EnterReadLock();
        try
        {
            var roomMessages = _messages
                .Where(m => m.RoomId == roomId)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToList()
                .AsReadOnly();
            
            return Task.FromResult<IReadOnlyList<ChatMessage>>(roomMessages);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<int> GetRoomMessageCountAsync(string roomId)
    {
        _lock.EnterReadLock();
        try
        {
            var count = _messages.Count(m => m.RoomId == roomId);
            return Task.FromResult(count);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<Dictionary<string, int>> GetAllRoomMessageCountsAsync()
    {
        _lock.EnterReadLock();
        try
        {
            var counts = new Dictionary<string, int>();
            foreach (var msg in _messages)
            {
                if (!counts.ContainsKey(msg.RoomId))
                {
                    counts[msg.RoomId] = 0;
                }
                counts[msg.RoomId]++;
            }
            return Task.FromResult(counts);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
