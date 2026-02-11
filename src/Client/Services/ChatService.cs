using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using SignalRDemo.Shared.Models;
using System.ComponentModel;

namespace SignalRDemo.Client.Services;

/// <summary>
/// 连接状态枚举
/// </summary>
public enum ConnectionState
{
    [Description("未连接")]
    Disconnected,
    
    [Description("连接中...")]
    Connecting,
    
    [Description("已连接")]
    Connected,
    
    [Description("重连中...")]
    Reconnecting,
    
    [Description("连接失败")]
    Failed
}

/// <summary>
/// SignalR 聊天服务 - 管理连接和消息收发
/// </summary>
public class ChatService : IAsyncDisposable, INotifyPropertyChanged
{
    private HubConnection? _hubConnection;
    private string _currentUser = string.Empty;
    private ConnectionState _state = ConnectionState.Disconnected;
    private readonly DebounceService _debounceService = new();
    private readonly ThrottleService _throttleService = new();
    private readonly ILogger<ChatService> _logger;
    
    // 节流相关事件
    public event Action<bool>? ThrottledChanged;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatService(ILogger<ChatService> logger)
    {
        _logger = logger;
    }

    // 事件
    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? UserJoined;
    public event Action<string>? UserLeft;
    public event Action<IReadOnlyList<string>>? UserListUpdated;
    public event Action<IReadOnlyList<ChatMessage>>? MessageHistoryReceived;
    public event Action<string>? DefaultUserNameReceived;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<Exception?>? ConnectionClosed;
    public event Action<string>? Reconnected;

    /// <summary>
    /// 当前连接状态
    /// </summary>
    public ConnectionState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StateText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnecting)));
            }
        }
    }

    /// <summary>
    /// 连接状态文本
    /// </summary>
    public string StateText => GetEnumDescription(State);

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => State == ConnectionState.Connected;

    /// <summary>
    /// 是否正在连接
    /// </summary>
    public bool IsConnecting => State == ConnectionState.Connecting || State == ConnectionState.Reconnecting;

    /// <summary>
    /// 当前用户名
    /// </summary>
    public string CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (_currentUser != value)
            {
                _currentUser = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUser)));
            }
        }
    }

    /// <summary>
    /// 初始化连接（幂等操作）
    /// </summary>
    public async Task InitializeAsync(string hubUrl)
    {
        // 如果已经连接或正在连接，直接返回
        if (_hubConnection != null && 
            (_hubConnection.State != HubConnectionState.Disconnected))
        {
            return;
        }

        State = ConnectionState.Connecting;

        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect(new[] { 
                    TimeSpan.Zero,       // 立即重试
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .AddMessagePackProtocol() // 使用 MessagePack 协议提升性能
                .Build();

            // 设置消息处理
            SetupEventHandlers();

            // 设置连接状态变更事件
            _hubConnection.Reconnecting += error =>
            {
                State = ConnectionState.Reconnecting;
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                State = ConnectionState.Connected;
                Reconnected?.Invoke(connectionId ?? string.Empty);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                State = ConnectionState.Disconnected;
                ConnectionClosed?.Invoke(error);
                return Task.CompletedTask;
            };

            // 启动连接
            await _hubConnection.StartAsync();
            State = ConnectionState.Connected;

            // 请求历史消息
            await GetRecentMessagesAsync();
        }
        catch (Exception ex)
        {
            State = ConnectionState.Failed;
            throw new InvalidOperationException($"Failed to initialize SignalR connection: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 重新连接
    /// </summary>
    public async Task ReconnectAsync(string hubUrl)
    {
        await DisposeAsync();
        await InitializeAsync(hubUrl);
    }

    /// <summary>
    /// 发送消息（带防抖和节流）
    /// </summary>
    public async Task SendMessageAsync(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText) || !IsConnected)
        {
            return;
        }

        // 防抖：100ms 内快速点击只发送最后一条
        await _debounceService.DebounceAsync(async () =>
        {
            // 节流：每 1 秒最多发送 1 条消息（防止刷屏）
            var sent = _throttleService.Throttle(async () =>
            {
                if (_hubConnection?.State != HubConnectionState.Connected)
                {
                    _logger.LogWarning("SignalR connection is not established. Message not sent.");
                    return;
                }

                try
                {
                    var chatMessage = new ChatMessage
                    {
                        User = CurrentUser,
                        Message = messageText.Trim(),
                        Timestamp = DateTime.UtcNow
                    };

                    await _hubConnection.SendAsync("SendMessage", chatMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send message");
                    throw;
                }
            }, intervalMilliseconds: 1000); // 1秒节流间隔
            
            // 通知 UI 是否被节流（false = 已发送，true = 被阻止）
            ThrottledChanged?.Invoke(!sent);
        }, delayMilliseconds: 100); // 100ms 防抖
    }

    /// <summary>
    /// 设置用户名
    /// </summary>
    public async Task SetUserNameAsync(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName) || !IsConnected)
        {
            return;
        }

        CurrentUser = userName.Trim();
        
        try
        {
            await _hubConnection!.SendAsync("SetUserName", CurrentUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user name: {UserName}", userName);
        }
    }

    /// <summary>
    /// 获取最近的消息历史
    /// </summary>
    public async Task GetRecentMessagesAsync(int count = 50)
    {
        if (!IsConnected) return;

        try
        {
            await _hubConnection!.SendAsync("GetRecentMessages", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent messages");
        }
    }

    /// <summary>
    /// 设置事件处理器
    /// </summary>
    private void SetupEventHandlers()
    {
        if (_hubConnection == null) return;

        // 接收消息
        _hubConnection.On<ChatMessage>("ReceiveMessage", message =>
        {
            MessageReceived?.Invoke(message);
        });

        // 接收消息历史
        _hubConnection.On<IReadOnlyList<ChatMessage>>("ReceiveMessageHistory", messages =>
        {
            MessageHistoryReceived?.Invoke(messages);
        });

        // 用户加入
        _hubConnection.On<string>("UserJoined", userName =>
        {
            UserJoined?.Invoke(userName);
        });

        // 用户离开
        _hubConnection.On<string>("UserLeft", userName =>
        {
            UserLeft?.Invoke(userName);
        });

        // 用户列表更新
        _hubConnection.On<IReadOnlyList<string>>("UpdateUserList", userNames =>
        {
            UserListUpdated?.Invoke(userNames);
        });

        // 默认用户名
        _hubConnection.On<string>("SetDefaultUserName", userName =>
        {
            CurrentUser = userName;
            DefaultUserNameReceived?.Invoke(userName);
        });
    }

    /// <summary>
    /// 获取枚举描述
    /// </summary>
    private static string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
            .FirstOrDefault() as DescriptionAttribute;
        return attribute?.Description ?? value.ToString();
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _debounceService.Dispose();
        
        if (_hubConnection != null)
        {
            // 移除所有事件处理
            _hubConnection.Remove("ReceiveMessage");
            _hubConnection.Remove("ReceiveMessageHistory");
            _hubConnection.Remove("UserJoined");
            _hubConnection.Remove("UserLeft");
            _hubConnection.Remove("UpdateUserList");
            _hubConnection.Remove("SetDefaultUserName");

            try
            {
                await _hubConnection.DisposeAsync();
            }
            finally
            {
                _hubConnection = null;
                State = ConnectionState.Disconnected;
            }
        }
    }
}
