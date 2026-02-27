using SignalRDemo.Client.Infrastructure;
using SignalRDemo.Client.Services.Abstractions;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Client.Services;

/// <summary>
/// 消息服务实现 - 管理消息业务逻辑
/// </summary>
public class MessageService : IMessageService
{
    private readonly ISignalRConnectionService _connectionService;
    private readonly IUserStateService _userStateService;
    private readonly ILogger<MessageService> _logger;
    private readonly ThrottleService _throttleService;

    private readonly List<ChatMessage> _currentMessages = new();
    private readonly Dictionary<string, List<ChatMessage>> _roomMessages = new();
    private string? _currentRoomId;

    public IReadOnlyList<ChatMessage> CurrentMessages => _currentMessages.AsReadOnly();
    public bool IsLoadingHistory { get; private set; }

    public event Action<ChatMessage>? MessageReceived;
    public event Action<IReadOnlyList<ChatMessage>>? MessageHistoryLoaded;
    public event Action<bool>? ThrottledChanged;

    public MessageService(
        ISignalRConnectionService connectionService,
        IUserStateService userStateService,
        ILogger<MessageService> logger)
    {
        _connectionService = connectionService;
        _userStateService = userStateService;
        _logger = logger;
        _throttleService = new ThrottleService();
    }

    public Task InitializeAsync()
    {
        // 注册 SignalR 事件处理
        _connectionService.On<ChatMessage>("ReceiveMessage", OnMessageReceived);
        _connectionService.On<IReadOnlyList<ChatMessage>>("ReceiveMessageHistory", OnMessageHistoryReceived);

        return Task.CompletedTask;
    }

    public async Task LoadMessageHistoryAsync(string roomId, int count = 50)
    {
        if (!_userStateService.IsLoggedIn) return;

        IsLoadingHistory = true;

        try
        {
            var messages = await _connectionService.InvokeAsync<List<ChatMessage>>("GetRoomMessages", roomId, count);
            if (messages != null)
            {
                // 按房间存储消息历史
                _roomMessages[roomId] = messages.ToList();
                
                // 如果是当前房间，更新当前消息列表
                if (_currentRoomId == roomId)
                {
                    _currentMessages.Clear();
                    _currentMessages.AddRange(messages);
                    MessageHistoryLoaded?.Invoke(_currentMessages.AsReadOnly());
                }
                
                _logger.LogInformation("加载房间 {RoomId} 消息历史: {Count} 条", roomId, messages.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载消息历史失败");
        }
        finally
        {
            IsLoadingHistory = false;
        }
    }

    public async Task SendMessageAsync(string roomId, string message, MessageType type = MessageType.Text)
    {
        if (string.IsNullOrWhiteSpace(message) || !_userStateService.IsLoggedIn)
        {
            return;
        }

        const int maxMessageLength = 500;
        if (message.Length > maxMessageLength)
        {
            message = message[..maxMessageLength];
        }

        var request = new SendMessageRequest
        {
            RoomId = roomId,
            Message = message.Trim(),
            Type = type
        };

        var sent = _throttleService.Throttle(async () =>
        {
            try
            {
                await _connectionService.InvokeAsync("SendRoomMessage", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息失败");
            }
        }, intervalMilliseconds: 1000);

        ThrottledChanged?.Invoke(!sent);
    }

    public async Task SwitchRoomAsync(string roomId)
    {
        _currentRoomId = roomId;
        
        // 清空当前消息
        _currentMessages.Clear();
        
        // 尝试从缓存恢复消息
        if (_roomMessages.TryGetValue(roomId, out var cachedMessages))
        {
            _currentMessages.AddRange(cachedMessages);
            MessageHistoryLoaded?.Invoke(_currentMessages.AsReadOnly());
        }
        else
        {
            // 加载消息历史
            await LoadMessageHistoryAsync(roomId);
        }
        
        _logger.LogInformation("切换到房间: {RoomId}", roomId);
    }

    public void ClearMessages()
    {
        _currentMessages.Clear();
    }

    private void OnMessageReceived(ChatMessage message)
    {
        // 将消息添加到对应房间的缓存
        if (!_roomMessages.ContainsKey(message.RoomId))
        {
            _roomMessages[message.RoomId] = new List<ChatMessage>();
        }
        _roomMessages[message.RoomId].Add(message);

        // 如果是当前房间的消息，添加到当前消息列表
        if (_currentRoomId == message.RoomId)
        {
            _currentMessages.Add(message);
        }

        MessageReceived?.Invoke(message);
        _logger.LogDebug("收到消息: {User} - {MessagePreview}", 
            message.UserName, 
            message.Message[..Math.Min(30, message.Message.Length)]);
    }

    private void OnMessageHistoryReceived(IReadOnlyList<ChatMessage> messages)
    {
        // 此方法用于兼容旧的全局消息历史
        // 新逻辑使用 GetRoomMessages
        _logger.LogDebug("收到消息历史: {Count} 条", messages.Count);
    }
}
