using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.JSInterop;
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
/// SignalR 聊天服务 - 管理连接、消息收发、用户认证和房间管理
/// </summary>
public class ChatService : IAsyncDisposable, INotifyPropertyChanged
{
    private HubConnection? _hubConnection;
    private string? _hubUrl;
    private string _currentUserName = string.Empty;
    private string _currentUserId = string.Empty;
    private User? _currentUser;
    private ConnectionState _state = ConnectionState.Disconnected;
    private string? _currentRoomId;
    private readonly DebounceService _debounceService = new();
    private readonly ThrottleService _throttleService = new();
    private readonly ILogger<ChatService> _logger;
    private readonly IJSRuntime _jsRuntime;
    
    // 节流相关事件
    public event Action<bool>? ThrottledChanged;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ChatService(ILogger<ChatService> logger, IJSRuntime jsRuntime)
    {
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// 获取存储的 JWT Token
    /// </summary>
    private async Task<string?> GetAccessTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "AccessToken");
        }
        catch
        {
            return null;
        }
    }

    #region 事件

    // 基础事件
    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? UserJoined;
    public event Action<string>? UserLeft;
    public event Action<IReadOnlyList<string>>? UserListUpdated;
    public event Action<IReadOnlyList<ChatMessage>>? MessageHistoryReceived;
    public event Action<string>? DefaultUserNameReceived;
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<Exception?>? ConnectionClosed;
    public event Action<string>? Reconnected;

    // 认证事件
    public event Action<User?>? AuthStateChanged;
    public event Action<dynamic>? DisplayNameResult;

    // 房间事件
    public event Action<JoinRoomResponse>? RoomCreated;
    public event Action<JoinRoomResponse>? JoinRoomResult;
    // public event Action<string>? LeaveRoomResult; // 未使用
    public event Action<IReadOnlyList<ChatRoom>>? RoomsUpdated;
    public event Action<IReadOnlyList<ChatRoom>>? MyRoomsUpdated;
    public event Action<ChatRoom?>? CurrentRoomChanged;
    public event Action<IReadOnlyList<string>>? RoomUserListUpdated;

    #endregion

    #region 属性

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
    /// 当前用户名（显示用）
    /// </summary>
    public string CurrentUserName
    {
        get => _currentUserName;
        private set
        {
            if (_currentUserName != value)
            {
                _currentUserName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUserName)));
            }
        }
    }

    /// <summary>
    /// 当前用户ID
    /// </summary>
    public string CurrentUserId
    {
        get => _currentUserId;
        private set
        {
            if (_currentUserId != value)
            {
                _currentUserId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUserId)));
            }
        }
    }

    /// <summary>
    /// 当前用户信息
    /// </summary>
    public User? CurrentUser
    {
        get => _currentUser;
        private set
        {
            if (_currentUser != value)
            {
                _currentUser = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUser)));
                AuthStateChanged?.Invoke(value);
            }
        }
    }

    /// <summary>
    /// 是否已登录
    /// </summary>
    public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUserId);

    /// <summary>
    /// 当前房间ID
    /// </summary>
    public string? CurrentRoomId
    {
        get => _currentRoomId;
        private set
        {
            if (_currentRoomId != value)
            {
                _currentRoomId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentRoomId)));
            }
        }
    }

    /// <summary>
    /// 当前房间
    /// </summary>
    public ChatRoom? CurrentRoom { get; private set; }

    #endregion

    #region 连接管理

    /// <summary>
    /// 初始化连接（幂等操作）
    /// </summary>
    public async Task InitializeAsync(string hubUrl)
    {
        // 保存 hubUrl 以便后续重新初始化
        _hubUrl = hubUrl;

        if (_hubConnection != null && 
            (_hubConnection.State != HubConnectionState.Disconnected))
        {
            return;
        }

        State = ConnectionState.Connecting;

        try
        {
            // 获取 JWT Token（如果有的话）
            var accessToken = await GetAccessTokenAsync();
            
            var hubConnectionBuilder = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(accessToken);
                    }
                })
                .WithAutomaticReconnect(new[] { 
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .AddMessagePackProtocol();
            
            _hubConnection = hubConnectionBuilder.Build();

            SetupEventHandlers();

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

            await _hubConnection.StartAsync();
            State = ConnectionState.Connected;

            // 连接成功后，主动获取在线用户列表
            await GetOnlineUsersAsync();
            
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
    /// 重新初始化 SignalR 连接（用于登录后传递 JWT Token）
    /// </summary>
    public async Task ReinitializeConnectionAsync()
    {
        var hubUrl = _hubUrl;
        if (string.IsNullOrEmpty(hubUrl))
        {
            return;
        }

        // 如果已连接，先断开
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
            catch
            {
                // 忽略断开错误
            }
            _hubConnection = null;
        }

        // 重新初始化连接
        await InitializeAsync(hubUrl);
    }

    #endregion

    #region 用户认证

    /// <summary>
    /// 设置当前用户（登录成功后由 UI 层调用）
    /// </summary>
    public void SetCurrentUser(User user)
    {
        CurrentUser = user;
        CurrentUserId = user.Id;
        CurrentUserName = user.UserName;
    }

    /// <summary>
    /// 清除当前用户（退出登录时调用）
    /// </summary>
    public void ClearCurrentUser()
    {
        CurrentUser = null;
        CurrentUserId = string.Empty;
        CurrentUserName = string.Empty;
    }

    /// <summary>
    /// 设置显示昵称（仅 SignalR 通信用）
    /// </summary>
    public async Task SetDisplayNameAsync(string displayName)
    {
        if (!IsConnected || !IsLoggedIn) return;

        try
        {
            await _hubConnection!.SendAsync("SetDisplayName", displayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set display name");
        }
    }

    #endregion

    #region 房间管理

    /// <summary>
    /// 创建房间
    /// </summary>
    public async Task CreateRoomAsync(string name, string? description, bool isPublic, string? password)
    {
        if (!IsConnected || !IsLoggedIn) return;

        var request = new CreateRoomRequest
        {
            Name = name,
            Description = description,
            IsPublic = isPublic,
            Password = isPublic ? null : password
        };

        try
        {
            await _hubConnection!.SendAsync("CreateRoom", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create room");
            RoomCreated?.Invoke(new JoinRoomResponse
            {
                Success = false,
                Message = "创建房间失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 获取公共房间列表
    /// </summary>
    public async Task<List<ChatRoom>> GetRoomsAsync()
    {
        if (!IsConnected) return new List<ChatRoom>();

        try
        {
            return await _hubConnection!.InvokeAsync<List<ChatRoom>>("GetRooms") ?? new List<ChatRoom>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get rooms");
            return new List<ChatRoom>();
        }
    }

    /// <summary>
    /// 获取我的房间列表
    /// </summary>
    public async Task<List<ChatRoom>> GetMyRoomsAsync()
    {
        if (!IsConnected || !IsLoggedIn) return new List<ChatRoom>();

        try
        {
            return await _hubConnection!.InvokeAsync<List<ChatRoom>>("GetMyRooms") ?? new List<ChatRoom>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get my rooms");
            return new List<ChatRoom>();
        }
    }

    /// <summary>
    /// 通过房间名称加入房间
    /// </summary>
    public async Task<bool> JoinRoomByNameAsync(string roomName, string? password = null)
    {
        if (!IsConnected || !IsLoggedIn) return false;

        try
        {
            var response = await _hubConnection!.InvokeAsync<JoinRoomResponse>("JoinRoomByName", roomName, password);
            if (response?.Success == true)
            {
                CurrentRoomId = response.Room?.Id;
                CurrentRoom = response.Room;
                CurrentRoomChanged?.Invoke(response.Room);
                return true;
            }
            else
            {
                JoinRoomResult?.Invoke(response ?? new JoinRoomResponse
                {
                    Success = false,
                    Message = "加入房间失败"
                });
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join room by name");
            JoinRoomResult?.Invoke(new JoinRoomResponse
            {
                Success = false,
                Message = "加入房间失败，请稍后重试"
            });
            return false;
        }
    }

    /// <summary>
    /// 根据房间名称搜索房间
    /// </summary>
    public async Task<List<ChatRoom>> SearchRoomsByNameAsync(string roomName)
    {
        if (!IsConnected) return new List<ChatRoom>();

        try
        {
            return await _hubConnection!.InvokeAsync<List<ChatRoom>>("SearchRoomsByName", roomName) 
                   ?? new List<ChatRoom>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search rooms by name");
            return new List<ChatRoom>();
        }
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public async Task<bool> JoinRoomAsync(string roomId, string? password = null)
    {
        if (!IsConnected || !IsLoggedIn) return false;

        var request = new JoinRoomRequest
        {
            RoomId = roomId,
            Password = password
        };

        try
        {
            var response = await _hubConnection!.InvokeAsync<JoinRoomResponse>("JoinRoom", request);
            if (response?.Success == true)
            {
                CurrentRoomId = roomId;
                CurrentRoom = response.Room;
                CurrentRoomChanged?.Invoke(response.Room);
                return true;
            }
            else
            {
                JoinRoomResult?.Invoke(response ?? new JoinRoomResponse
                {
                    Success = false,
                    Message = "加入房间失败"
                });
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join room");
            JoinRoomResult?.Invoke(new JoinRoomResponse
            {
                Success = false,
                Message = "加入房间失败，请稍后重试"
            });
            return false;
        }
    }

    /// <summary>
    /// 离开当前房间
    /// </summary>
    public async Task LeaveRoomAsync()
    {
        if (!IsConnected || string.IsNullOrEmpty(CurrentRoomId)) return;

        try
        {
            await _hubConnection!.SendAsync("LeaveRoom", CurrentRoomId);
            CurrentRoomId = null;
            CurrentRoom = null;
            CurrentRoomChanged?.Invoke(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave room");
        }
    }

    /// <summary>
    /// 验证房间密码
    /// </summary>
    public async Task<bool> VerifyRoomPasswordAsync(string roomId, string password)
    {
        if (!IsConnected) return false;

        try
        {
            return await _hubConnection!.InvokeAsync<bool>("VerifyRoomPassword", roomId, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify room password");
            return false;
        }
    }

    #endregion

    #region 消息

    /// <summary>
    /// 发送全局消息（保持兼容）
    /// </summary>
    public async Task SendMessageAsync(string messageText)
    {
        if (string.IsNullOrWhiteSpace(messageText) || !IsConnected)
        {
            return;
        }

        await _debounceService.DebounceAsync(async () =>
        {
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
                        User = CurrentUserName,
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
            }, intervalMilliseconds: 1000);
            
            ThrottledChanged?.Invoke(!sent);
        }, delayMilliseconds: 100);
    }

    /// <summary>
    /// 发送房间消息
    /// </summary>
    public async Task SendRoomMessageAsync(string roomId, string message, MessageType type = MessageType.Text)
    {
        if (string.IsNullOrWhiteSpace(message) || !IsConnected)
        {
            return;
        }

        var sent = _throttleService.Throttle(async () =>
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                var request = new SendMessageRequest
                {
                    RoomId = roomId,
                    Message = message.Trim(),
                    Type = type
                };

                await _hubConnection.SendAsync("SendRoomMessage", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send room message");
            }
        }, intervalMilliseconds: 1000);

        ThrottledChanged?.Invoke(!sent);
    }

    /// <summary>
    /// 获取房间消息历史
    /// </summary>
    public async Task<List<ChatMessage>> GetRoomMessagesAsync(string roomId, int count = 50)
    {
        if (!IsConnected) return new List<ChatMessage>();

        try
        {
            return await _hubConnection!.InvokeAsync<List<ChatMessage>>("GetRoomMessages", roomId, count) 
                   ?? new List<ChatMessage>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get room messages");
            return new List<ChatMessage>();
        }
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

        CurrentUserName = userName.Trim();
        
        try
        {
            await _hubConnection!.SendAsync("SetUserName", CurrentUserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set user name: {UserName}", userName);
        }
    }

    /// <summary>
    /// 获取在线用户列表
    /// </summary>
    public async Task GetOnlineUsersAsync()
    {
        if (!IsConnected) return;

        try
        {
            await _hubConnection!.SendAsync("GetOnlineUsers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get online users");
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

    #endregion

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
            CurrentUserName = userName;
            DefaultUserNameReceived?.Invoke(userName);
        });

        // 显示昵称设置结果
        _hubConnection.On<dynamic>("DisplayNameResult", result =>
        {
            DisplayNameResult?.Invoke(result);
        });

        // 房间创建结果
        _hubConnection.On<JoinRoomResponse>("RoomCreated", response =>
        {
            RoomCreated?.Invoke(response);
        });

        // 加入房间结果
        _hubConnection.On<JoinRoomResponse>("JoinRoomResult", response =>
        {
            JoinRoomResult?.Invoke(response);
        });

        // 房间列表更新
        _hubConnection.On<IReadOnlyList<ChatRoom>>("RoomsUpdated", rooms =>
        {
            RoomsUpdated?.Invoke(rooms);
        });

        // 我的房间列表更新
        _hubConnection.On<IReadOnlyList<ChatRoom>>("MyRoomsUpdated", rooms =>
        {
            MyRoomsUpdated?.Invoke(rooms);
        });

        // 房间用户列表更新
        _hubConnection.On<IReadOnlyList<string>>("RoomUserListUpdated", userNames =>
        {
            RoomUserListUpdated?.Invoke(userNames);
        });

        // 错误处理
        _hubConnection.On<string>("Error", errorMessage =>
        {
            _logger.LogWarning("Server error: {Error}", errorMessage);
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
            _hubConnection.Remove("DisplayNameResult");
            _hubConnection.Remove("RoomCreated");
            _hubConnection.Remove("JoinRoomResult");
            _hubConnection.Remove("RoomsUpdated");
            _hubConnection.Remove("MyRoomsUpdated");
            _hubConnection.Remove("Error");

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
