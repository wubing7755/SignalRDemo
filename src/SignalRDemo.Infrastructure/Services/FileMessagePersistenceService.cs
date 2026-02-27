using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 文件消息持久化服务实现
/// 按房间+日期分区存储，支持索引优化
/// </summary>
public class FileMessagePersistenceService : IFileMessagePersistenceService
{
    private readonly string _basePath;
    private readonly ILogger<FileMessagePersistenceService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    
    // 目录名称
    private const string ByRoomFolder = "by-room";
    private const string ByDateFolder = "by-date";
    private const string IndexFolder = "indexes";
    private const string ArchiveFolder = "archive";

    public FileMessagePersistenceService(
        IConfiguration configuration,
        ILogger<FileMessagePersistenceService> logger)
    {
        _basePath = configuration["MessageStorage:Path"] 
            ?? Path.Combine(AppContext.BaseDirectory, "messages");
        _logger = logger;
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
        
        EnsureDirectoryStructure();
    }

    private void EnsureDirectoryStructure()
    {
        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(Path.Combine(_basePath, ByRoomFolder));
        Directory.CreateDirectory(Path.Combine(_basePath, ByDateFolder));
        Directory.CreateDirectory(Path.Combine(_basePath, IndexFolder));
        Directory.CreateDirectory(Path.Combine(_basePath, ArchiveFolder));
        
        _logger.LogInformation("消息存储目录已初始化: {Path}", _basePath);
    }

    private string GetRoomFilePath(string roomId, DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        var roomFolder = Path.Combine(_basePath, ByRoomFolder, roomId);
        Directory.CreateDirectory(roomFolder);
        return Path.Combine(roomFolder, $"{dateStr}.json");
    }

    private string GetDateFilePath(DateTime date)
    {
        var dateStr = date.ToString("yyyy-MM-dd");
        return Path.Combine(_basePath, ByDateFolder, $"{dateStr}.json");
    }

    private string GetIndexFilePath()
    {
        return Path.Combine(_basePath, IndexFolder, "message-index.json");
    }

    public async Task PersistAsync(ChatMessage message)
    {
        await _fileLock.WaitAsync();
        try
        {
            var date = message.Timestamp.Date;
            
            // 1. 按房间+日期存储
            var roomFilePath = GetRoomFilePath(message.RoomId, date);
            await AppendToFileAsync(roomFilePath, message);
            
            // 2. 按日期归档
            var dateFilePath = GetDateFilePath(date);
            await AppendToFileAsync(dateFilePath, message);
            
            // 3. 更新索引
            await UpdateIndexAsync(message);
            
            _logger.LogDebug("消息已持久化: {MessageId} -> {RoomId}", 
                message.Id, message.RoomId);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task PersistBatchAsync(IEnumerable<ChatMessage> messages)
    {
        var messageList = messages.ToList();
        if (!messageList.Any()) return;
        
        await _fileLock.WaitAsync();
        try
        {
            // 按日期分组
            var groupedByDate = messageList.GroupBy(m => m.Timestamp.Date);
            
            foreach (var group in groupedByDate)
            {
                var date = group.Key;
                
                // 按房间分组
                var groupedByRoom = group.GroupBy(m => m.RoomId);
                
                foreach (var roomGroup in groupedByRoom)
                {
                    var roomFilePath = GetRoomFilePath(roomGroup.Key, date);
                    var existingMessages = await LoadMessagesAsync(roomFilePath);
                    existingMessages.AddRange(roomGroup);
                    await SaveMessagesAsync(roomFilePath, existingMessages);
                }
                
                // 日期文件追加
                var dateFilePath = GetDateFilePath(date);
                var existingDateMessages = await LoadMessagesAsync(dateFilePath);
                existingDateMessages.AddRange(group);
                await SaveMessagesAsync(dateFilePath, existingDateMessages);
            }
            
            // 批量更新索引
            foreach (var message in messageList)
            {
                await UpdateIndexAsync(message);
            }
            
            _logger.LogInformation("批量消息已持久化: {Count} 条", messageList.Count);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<ChatMessage>> GetRoomMessagesAsync(string roomId, int offset, int count)
    {
        var messages = new List<ChatMessage>();
        var roomFolder = Path.Combine(_basePath, ByRoomFolder, roomId);
        
        if (!Directory.Exists(roomFolder))
        {
            return messages;
        }
        
        // 获取房间所有消息文件，按日期倒序
        var files = Directory.GetFiles(roomFolder, "*.json")
            .OrderByDescending(f => Path.GetFileNameWithoutExtension(f))
            .ToList();
        
        int skipped = 0;
        int taken = 0;
        
        foreach (var file in files)
        {
            if (taken >= count) break;
            
            var fileMessages = await LoadMessagesAsync(file);
            var totalInFile = fileMessages.Count;
            
            // 计算需要跳过的数量
            if (skipped + totalInFile <= offset)
            {
                skipped += totalInFile;
                continue;
            }
            
            // 计算从当前文件取多少
            var startIndex = Math.Max(0, offset - skipped);
            var remaining = count - taken;
            var takeCount = Math.Min(remaining, totalInFile - startIndex);
            
            messages.AddRange(fileMessages.Skip(startIndex).Take(takeCount));
            taken += takeCount;
            skipped += startIndex;
        }
        
        return messages;
    }

    public async Task<long> GetRoomMessageCountAsync(string roomId)
    {
        var roomFolder = Path.Combine(_basePath, ByRoomFolder, roomId);
        
        if (!Directory.Exists(roomFolder))
        {
            return 0;
        }
        
        var files = Directory.GetFiles(roomFolder, "*.json");
        long total = 0;
        
        foreach (var file in files)
        {
            var messages = await LoadMessagesAsync(file);
            total += messages.Count;
        }
        
        return total;
    }

    public async Task<List<string>> GetAllRoomIdsAsync()
    {
        var roomFolder = Path.Combine(_basePath, ByRoomFolder);
        
        if (!Directory.Exists(roomFolder))
        {
            return new List<string>();
        }
        
        return Directory.GetDirectories(roomFolder)
            .Select(d => Path.GetFileName(d))
            .Where(n => n != null)
            .Cast<string>()
            .ToList();
    }

    public async Task RebuildIndexAsync()
    {
        _logger.LogInformation("开始重建消息索引...");
        
        var index = new Dictionary<string, MessageIndexEntry>();
        var roomFolder = Path.Combine(_basePath, ByRoomFolder);
        
        if (!Directory.Exists(roomFolder))
        {
            return;
        }
        
        foreach (var roomDir in Directory.GetDirectories(roomFolder))
        {
            var roomId = Path.GetFileName(roomDir);
            
            foreach (var file in Directory.GetFiles(roomDir, "*.json"))
            {
                var messages = await LoadMessagesAsync(file);
                
                foreach (var message in messages)
                {
                    index[message.Id] = new MessageIndexEntry
                    {
                        MessageId = message.Id,
                        RoomId = roomId,
                        Timestamp = message.Timestamp,
                        FilePath = file
                    };
                }
            }
        }
        
        var indexFile = GetIndexFilePath();
        var indexJson = JsonSerializer.Serialize(index.Values.ToList(), _jsonOptions);
        await File.WriteAllTextAsync(indexFile, indexJson);
        
        _logger.LogInformation("索引重建完成: {Count} 条记录", index.Count);
    }

    public async Task ArchiveOldMessagesAsync(int daysToKeep)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var roomFolder = Path.Combine(_basePath, ByRoomFolder);
        
        if (!Directory.Exists(roomFolder))
        {
            return;
        }
        
        foreach (var roomDir in Directory.GetDirectories(roomFolder))
        {
            var roomId = Path.GetFileName(roomDir);
            
            foreach (var file in Directory.GetFiles(roomDir, "*.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (DateTime.TryParse(fileName, out var fileDate) && fileDate < cutoffDate)
                {
                    // 移动到归档目录
                    var archivePath = Path.Combine(_basePath, ArchiveFolder, 
                        roomId, fileDate.ToString("yyyy"));
                    Directory.CreateDirectory(archivePath);
                    
                    var destFile = Path.Combine(archivePath, Path.GetFileName(file));
                    File.Move(file, destFile, true);
                    
                    _logger.LogInformation("消息已归档: {RoomId} -> {ArchivePath}", 
                        roomId, destFile);
                }
            }
        }
        
        await Task.CompletedTask;
    }

    private async Task AppendToFileAsync(string filePath, ChatMessage message)
    {
        var messages = await LoadMessagesAsync(filePath);
        messages.Add(message);
        await SaveMessagesAsync(filePath, messages);
    }

    private async Task<List<ChatMessage>> LoadMessagesAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<ChatMessage>();
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<List<ChatMessage>>(json, _jsonOptions) 
                ?? new List<ChatMessage>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取消息文件失败: {FilePath}", filePath);
            return new List<ChatMessage>();
        }
    }

    private async Task SaveMessagesAsync(string filePath, List<ChatMessage> messages)
    {
        var json = JsonSerializer.Serialize(messages, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    private async Task UpdateIndexAsync(ChatMessage message)
    {
        try
        {
            var indexFile = GetIndexFilePath();
            Dictionary<string, MessageIndexEntry> index;
            
            if (File.Exists(indexFile))
            {
                var json = await File.ReadAllTextAsync(indexFile);
                index = JsonSerializer.Deserialize<List<MessageIndexEntry>>(json, _jsonOptions)
                    .ToDictionary(e => e.MessageId);
            }
            else
            {
                index = new Dictionary<string, MessageIndexEntry>();
            }
            
            index[message.Id] = new MessageIndexEntry
            {
                MessageId = message.Id,
                RoomId = message.RoomId,
                Timestamp = message.Timestamp,
                FilePath = GetRoomFilePath(message.RoomId, message.Timestamp.Date)
            };
            
            var indexJson = JsonSerializer.Serialize(index.Values.ToList(), _jsonOptions);
            await File.WriteAllTextAsync(indexFile, indexJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "更新索引失败: {MessageId}", message.Id);
        }
    }
}

/// <summary>
/// 消息索引条目
/// </summary>
public class MessageIndexEntry
{
    public string MessageId { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string FilePath { get; set; } = string.Empty;
}
