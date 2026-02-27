using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalRDemo.Shared.Models;
using SignalRDemo.Infrastructure.Services;

namespace SignalRDemo.Infrastructure.BackgroundServices;

/// <summary>
/// 消息持久化后台工作者
/// 从 Redis 队列消费消息并持久化到文件系统
/// </summary>
public class MessagePersistenceWorker : BackgroundService
{
    private readonly IMessageCacheService _cacheService;
    private readonly IFileMessagePersistenceService _persistenceService;
    private readonly ILogger<MessagePersistenceWorker> _logger;
    
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 500;

    public MessagePersistenceWorker(
        IMessageCacheService cacheService,
        IFileMessagePersistenceService persistenceService,
        ILogger<MessagePersistenceWorker> logger)
    {
        _cacheService = cacheService;
        _persistenceService = persistenceService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("消息持久化服务已启动");

        // 等待 Redis 连接就绪
        await Task.Delay(1000, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 从 Redis 队列获取消息
                var message = await _cacheService.DequeueAsync(stoppingToken);
                
                if (message != null)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 正常取消
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "消息处理循环发生错误");
                await Task.Delay(1000, stoppingToken); // 错误后等待
            }
        }

        _logger.LogInformation("消息持久化服务已停止");
    }

    private async Task ProcessMessageAsync(MessageCacheDto cacheDto, CancellationToken ct)
    {
        var retryCount = 0;
        
        while (retryCount < MaxRetries)
        {
            try
            {
                // 转换为领域模型
                var message = new ChatMessage
                {
                    Id = cacheDto.Id,
                    UserId = cacheDto.SenderId,
                    UserName = cacheDto.SenderName,
                    RoomId = cacheDto.RoomId,
                    Message = cacheDto.Content,
                    Type = Enum.Parse<SignalRDemo.Shared.Models.MessageType>(cacheDto.Type),
                    MediaUrl = cacheDto.MediaUrl,
                    AltText = cacheDto.AltText,
                    Timestamp = cacheDto.Timestamp
                };
                
                // 持久化到文件系统
                await _persistenceService.PersistAsync(message);
                
                _logger.LogDebug("消息已持久化: {MessageId} -> Room {RoomId}", 
                    message.Id, message.RoomId);
                
                return; // 成功处理
            }
            catch (IOException ex)
            {
                retryCount++;
                _logger.LogWarning(ex, "文件写入失败，{RetryCount}/{MaxRetries}", 
                    retryCount, MaxRetries);
                
                if (retryCount < MaxRetries)
                {
                    await Task.Delay(RetryDelayMs * retryCount, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "消息处理失败: {MessageId}", cacheDto.Id);
                break; // 非重试异常，放弃此消息
            }
        }

        // 所有重试都失败，记录错误日志
        _logger.LogError("消息持久化失败，已超过最大重试次数: {MessageId}", cacheDto.Id);
    }
}
