using Microsoft.AspNetCore.SignalR;
using SignalRDemo.Server.Services;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Hubs;

/// <summary>
/// 聊天 Hub - 处理实时消息、用户认证和房间管理
/// </summary>
public class ChatHub : Hub
{
    private readonly IChatRepository _chatRepository;
    private readonly IUserConnectionManager _connectionManager;
    private readonly IUserService _userService;
    private readonly IRoomService _roomService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatRepository chatRepository,
        IUserConnectionManager connectionManager,
        IUserService userService,
        IRoomService roomService,
        ILogger<ChatHub> logger)
    {
        _chatRepository = chatRepository;
        _connectionManager = connectionManager;
        _userService = userService;
        _roomService = roomService;
        _logger = logger;
    }

    #region 用户认证

    /// <summary>
    /// 用户注册
    /// </summary>
    public async Task Register(RegisterRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                await Clients.Caller.SendAsync("RegisterResult", new LoginResponse
                {
                    Success = false,
                    Message = "用户名和密码不能为空"
                });
                return;
            }

            var user = await _userService.RegisterAsync(request.UserName, request.Password, request.DisplayName);

            if (user == null)
            {
                await Clients.Caller.SendAsync("RegisterResult", new LoginResponse
                {
                    Success = false,
                    Message = "用户名已存在"
                });
                return;
            }

            // 更新连接管理器的用户信息
            _connectionManager.UpdateUserName(Context.ConnectionId, user.UserName);
            _connectionManager.SetUserId(Context.ConnectionId, user.Id);

            await Clients.Caller.SendAsync("RegisterResult", new LoginResponse
            {
                Success = true,
                Message = "注册成功",
                User = user
            });

            _logger.LogInformation("用户注册成功: {UserName} (ID: {UserId})", user.UserName, user.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户注册时发生错误");
            await Clients.Caller.SendAsync("RegisterResult", new LoginResponse
            {
                Success = false,
                Message = "注册失败，请稍后重试"
            });
        }
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<LoginResponse> Login(LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "用户名和密码不能为空"
                };
            }

            var user = await _userService.LoginAsync(request.UserName, request.Password);

            if (user == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "用户名或密码错误"
                };
            }

            // 更新连接管理器的用户信息
            _connectionManager.UpdateUserName(Context.ConnectionId, user.UserName);
            _connectionManager.SetUserId(Context.ConnectionId, user.Id);

            // 广播用户列表更新
            await BroadcastUserListAsync();

            _logger.LogInformation("用户登录成功: {UserName} (ID: {UserId})", user.UserName, user.Id);

            return new LoginResponse
            {
                Success = true,
                Message = "登录成功",
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登录时发生错误");
            return new LoginResponse
            {
                Success = false,
                Message = "登录失败，请稍后重试"
            };
        }
    }

    /// <summary>
    /// 设置显示昵称
    /// </summary>
    public async Task SetDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        const int maxLength = 30;
        if (displayName.Length > maxLength)
        {
            displayName = displayName[..maxLength];
        }

        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("DisplayNameResult", new { Success = false, Message = "请先登录" });
            return;
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user != null)
        {
            user.DisplayName = displayName;
            await Clients.Caller.SendAsync("DisplayNameResult", new { Success = true, DisplayName = displayName });
            await BroadcastUserListAsync();
        }
    }

    /// <summary>
    /// 登出
    /// </summary>
    public async Task Logout()
    {
        var userName = _connectionManager.GetUserName(Context.ConnectionId);
        _connectionManager.ClearUserId(Context.ConnectionId);

        await BroadcastUserListAsync();

        if (!string.IsNullOrEmpty(userName))
        {
            _logger.LogInformation("用户已登出: {UserName}", userName);
        }
    }

    #endregion

    #region 房间管理

    /// <summary>
    /// 创建房间
    /// </summary>
    public async Task<ChatRoom> CreateRoom(CreateRoomRequest request)
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("RoomCreated", new JoinRoomResponse
            {
                Success = false,
                Message = "请先登录"
            });
            return null!;
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
        {
            await Clients.Caller.SendAsync("RoomCreated", new JoinRoomResponse
            {
                Success = false,
                Message = "房间名称至少2个字符"
            });
            return null!;
        }

        if (!request.IsPublic && string.IsNullOrWhiteSpace(request.Password))
        {
            await Clients.Caller.SendAsync("RoomCreated", new JoinRoomResponse
            {
                Success = false,
                Message = "私人房间必须设置密码"
            });
            return null!;
        }

        var room = await _roomService.CreateRoomAsync(
            request.Name,
            request.Description,
            userId,
            request.IsPublic,
            request.IsPublic ? null : request.Password);

        // 创建者加入房间分组
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Id);

        // 广播新房间创建
        await Clients.All.SendAsync("RoomCreated", new JoinRoomResponse
        {
            Success = true,
            Message = $"房间 '{room.Name}' 创建成功",
            Room = room
        });

        _logger.LogInformation("房间创建成功: {RoomName} (ID: {RoomId}) by User {UserId}", 
            room.Name, room.Id, userId);

        return room;
    }

    /// <summary>
    /// 获取所有公共房间列表
    /// </summary>
    public async Task<List<ChatRoom>> GetRooms()
    {
        var rooms = await _roomService.GetPublicRoomsAsync();
        return rooms;
    }

    /// <summary>
    /// 获取我的房间列表
    /// </summary>
    public async Task<List<ChatRoom>> GetMyRooms()
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        if (string.IsNullOrEmpty(userId))
        {
            return new List<ChatRoom>();
        }

        var rooms = await _roomService.GetUserRoomsAsync(userId);
        return rooms;
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public async Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request)
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        var userName = _connectionManager.GetUserName(Context.ConnectionId);

        if (string.IsNullOrEmpty(userId))
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "请先登录"
            };
        }

        var room = await _roomService.GetRoomByIdAsync(request.RoomId);
        if (room == null)
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "房间不存在"
            };
        }

        // 验证密码（如果是私人房间）
        if (!room.IsPublic)
        {
            var isValid = await _roomService.VerifyPasswordAsync(request.RoomId, request.Password ?? string.Empty);
            if (!isValid)
            {
                return new JoinRoomResponse
                {
                    Success = false,
                    Message = "房间密码错误"
                };
            }
        }

        // 添加用户到房间
        var added = await _roomService.AddUserToRoomAsync(userId, request.RoomId);
        if (!added)
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "加入房间失败"
            };
        }

        // 加入 SignalR 分组
        await Groups.AddToGroupAsync(Context.ConnectionId, room.Id);

        // 广播用户加入消息
        var joinMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName ?? "未知用户",
            RoomId = room.Id,
            Message = $"欢迎 {userName ?? "未知用户"} 加入房间",
            Timestamp = DateTime.UtcNow
        };
        await Clients.Group(room.Id).SendAsync("ReceiveMessage", joinMessage);

        _logger.LogInformation("用户 {UserName} (ID: {UserId}) 加入房间 {RoomName} (ID: {RoomId})", 
            userName, userId, room.Name, room.Id);

        return new JoinRoomResponse
        {
            Success = true,
            Message = $"成功加入房间 '{room.Name}'",
            Room = room
        };
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    public async Task LeaveRoom(string roomId)
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        var userName = _connectionManager.GetUserName(Context.ConnectionId);

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var room = await _roomService.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            return;
        }

        // 从房间移除用户
        await _roomService.RemoveUserFromRoomAsync(userId, roomId);

        // 离开 SignalR 分组
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

        // 广播用户离开消息
        var leaveMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName ?? "未知用户",
            RoomId = roomId,
            Message = $"{userName ?? "未知用户"} 离开了房间",
            Timestamp = DateTime.UtcNow
        };
        await Clients.Group(roomId).SendAsync("ReceiveMessage", leaveMessage);

        _logger.LogInformation("用户 {UserName} (ID: {UserId}) 离开房间 {RoomName} (ID: {RoomId})", 
            userName, userId, room.Name, roomId);
    }

    /// <summary>
    /// 验证房间密码
    /// </summary>
    public async Task<bool> VerifyRoomPassword(string roomId, string password)
    {
        var room = await _roomService.GetRoomByIdAsync(roomId);
        if (room == null || room.IsPublic)
        {
            return false;
        }

        return await _roomService.VerifyPasswordAsync(roomId, password ?? string.Empty);
    }

    #endregion

    #region 消息

    /// <summary>
    /// 发送房间消息
    /// </summary>
    public async Task SendRoomMessage(SendMessageRequest request)
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        var userName = _connectionManager.GetUserName(Context.ConnectionId);

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return;
        }

        const int maxMessageLength = 500;
        if (request.Message.Length > maxMessageLength)
        {
            request.Message = request.Message[..maxMessageLength];
        }

        var room = await _roomService.GetRoomByIdAsync(request.RoomId);
        if (room == null)
        {
            return;
        }

        // 检查用户是否在房间中
        var isInRoom = await _roomService.IsUserInRoomAsync(userId, request.RoomId);
        if (!isInRoom)
        {
            await Clients.Caller.SendAsync("Error", "您不在该房间中");
            return;
        }

        var message = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName ?? "未知用户",
            RoomId = request.RoomId,
            Message = request.Message,
            Type = request.Type,
            MediaUrl = request.MediaUrl,
            AltText = request.AltText,
            Timestamp = DateTime.UtcNow
        };

        // 保存消息
        await _chatRepository.SaveRoomMessageAsync(message);

        // 广播给房间内所有用户
        await Clients.Group(request.RoomId).SendAsync("ReceiveMessage", message);

        _logger.LogDebug("房间消息已发送: {UserName} -> {RoomName}: {MessagePreview}", 
            userName, room.Name, message.Message[..Math.Min(50, message.Message.Length)]);
    }

    /// <summary>
    /// 获取房间消息历史
    /// </summary>
    public async Task<List<ChatMessage>> GetRoomMessages(string roomId, int count = 50)
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);

        if (string.IsNullOrEmpty(userId))
        {
            return new List<ChatMessage>();
        }

        var messages = await _chatRepository.GetRoomMessagesAsync(roomId, count);
        return messages.ToList();
    }

    #endregion

    #region 原有功能（保持兼容）

    /// <summary>
    /// 发送消息到所有客户端（全局消息，保持兼容）
    /// </summary>
    public async Task SendMessage(ChatMessage chatMessage)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chatMessage.Message))
            {
                return;
            }

            const int maxMessageLength = 500;
            if (chatMessage.Message.Length > maxMessageLength)
            {
                chatMessage.Message = chatMessage.Message[..maxMessageLength];
            }

            chatMessage.Timestamp = DateTime.UtcNow;
            
            await _chatRepository.SaveMessageAsync(chatMessage);
            
            await Clients.All.SendAsync("ReceiveMessage", chatMessage);
            
            _logger.LogDebug("全局消息已发送: {User} - {MessagePreview}", 
                chatMessage.User, 
                chatMessage.Message[..Math.Min(50, chatMessage.Message.Length)]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送消息时发生错误");
            throw;
        }
    }

    /// <summary>
    /// 设置用户名
    /// </summary>
    public async Task SetUserName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return;
        }

        const int maxUserNameLength = 20;
        if (userName.Length > maxUserNameLength)
        {
            userName = userName[..maxUserNameLength];
        }

        _connectionManager.UpdateUserName(Context.ConnectionId, userName);
        
        await BroadcastUserListAsync();
        
        _logger.LogInformation("用户设置名称: {UserName} (Connection: {ConnectionId})", 
            userName, Context.ConnectionId);
    }

    /// <summary>
    /// 获取最近的消息历史
    /// </summary>
    public async Task GetRecentMessages(int count = 50)
    {
        var messages = await _chatRepository.GetRecentMessagesAsync(count);
        await Clients.Caller.SendAsync("ReceiveMessageHistory", messages);
    }

    #endregion

    #region 连接事件

    /// <summary>
    /// 客户端连接时
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            var defaultUserName = $"User_{Guid.NewGuid().ToString()[..4]}";
            
            _connectionManager.AddConnection(Context.ConnectionId, defaultUserName);
            
            await Clients.Caller.SendAsync("SetDefaultUserName", defaultUserName);
            
            await BroadcastUserListAsync();
            
            await Clients.Others.SendAsync("UserJoined", defaultUserName);
            
            _logger.LogInformation("用户已连接: {UserName} (Connection: {ConnectionId})", 
                defaultUserName, Context.ConnectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理连接时发生错误: {ConnectionId}", Context.ConnectionId);
        }
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 客户端断开连接时
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            var userName = _connectionManager.GetUserName(Context.ConnectionId);
            var userId = _connectionManager.GetUserId(Context.ConnectionId);
            
            _connectionManager.RemoveConnection(Context.ConnectionId);
            
            await BroadcastUserListAsync();
            
            if (!string.IsNullOrEmpty(userName))
            {
                await Clients.All.SendAsync("UserLeft", userName);
            }
            
            if (exception != null)
            {
                _logger.LogWarning(exception, "用户异常断开: {UserName}", userName);
            }
            else
            {
                _logger.LogInformation("用户已断开: {UserName} (Connection: {ConnectionId})", 
                    userName, Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理断开连接时发生错误: {ConnectionId}", Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    /// <summary>
    /// 广播在线用户列表
    /// </summary>
    private async Task BroadcastUserListAsync()
    {
        var connections = _connectionManager.GetAllConnections();
        var userNames = connections.Select(c => c.UserName).ToList();
        await Clients.All.SendAsync("UpdateUserList", userNames);
    }
}
