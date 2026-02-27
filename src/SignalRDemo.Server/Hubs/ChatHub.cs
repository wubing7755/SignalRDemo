using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using SignalRDemo.Infrastructure.Services;
using SignalRDemo.Shared.Models;
using SignalRDemo.Application.Commands.Rooms;
using SignalRDemo.Application.Commands.Messages;
using SignalRDemo.Application.Commands.Users;
using SignalRDemo.Application.Queries.Rooms;
using SignalRDemo.Application.Queries.Messages;
using SignalRDemo.Application.DTOs;
using SignalRDemo.Application.Results;
using MediatR;

// 为避免类型冲突，使用显式命名空间别名
using RegisterRequest = SignalRDemo.Shared.Models.RegisterRequest;
using LoginRequest = SignalRDemo.Shared.Models.LoginRequest;
using LoginResponse = SignalRDemo.Shared.Models.LoginResponse;
using CreateRoomRequest = SignalRDemo.Shared.Models.CreateRoomRequest;
using JoinRoomRequest = SignalRDemo.Shared.Models.JoinRoomRequest;
using JoinRoomResponse = SignalRDemo.Shared.Models.JoinRoomResponse;
using SendMessageRequest = SignalRDemo.Shared.Models.SendMessageRequest;

namespace SignalRDemo.Server.Hubs;

/// <summary>
/// 聊天 Hub - 处理实时消息、用户认证和房间管理
/// 遵循 DDD + CQRS 架构模式
/// </summary>
public class ChatHub : Hub
{
    private readonly IUserConnectionManager _connectionManager;
    private readonly IMediator _mediator;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IUserConnectionManager connectionManager,
        IMediator mediator,
        ILogger<ChatHub> logger)
    {
        _connectionManager = connectionManager;
        _mediator = mediator;
        _logger = logger;
    }

    #region 用户认证

    /// <summary>
    /// 获取当前用户信息（统一从 ConnectionManager 获取，确保一致性）
    /// </summary>
    private (string? UserId, string? UserName) GetCurrentUser()
    {
        // 优先从 ConnectionManager 获取（SignalR 直连方式）
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        var userName = _connectionManager.GetUserName(Context.ConnectionId);
        
        // 如果 ConnectionManager 中没有，尝试从 Claims 获取（REST API + JWT 方式）
        if (string.IsNullOrEmpty(userId))
        {
            userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            userName = Context.User?.FindFirstValue(ClaimTypes.Name) 
                ?? Context.User?.FindFirstValue("display_name");
        }
        
        return (userId, userName);
    }

    /// <summary>
    /// 用户注册 - 使用DDD Command
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

            var command = new RegisterUserCommand(request.UserName, request.Password, request.DisplayName);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                await Clients.Caller.SendAsync("RegisterResult", new LoginResponse
                {
                    Success = false,
                    Message = result.Error ?? "用户名已存在"
                });
                return;
            }

            var userDto = result.Value;

            // 更新连接管理器的用户信息
            _connectionManager.UpdateUserName(Context.ConnectionId, userDto.UserName);
            _connectionManager.SetUserId(Context.ConnectionId, userDto.Id);

            // 转换为Shared.Models.User
            var user = new User
            {
                Id = userDto.Id,
                UserName = userDto.UserName,
                DisplayName = userDto.DisplayName
            };

            await Clients.Caller.SendAsync("RegisterResult", new LoginResponse
            {
                Success = true,
                Message = "注册成功",
                User = user
            });

            _logger.LogInformation("用户注册成功: {UserName} (ID: {UserId})", userDto.UserName, userDto.Id);
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
    /// 用户登录 - 使用DDD Command
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

            var command = new LoginCommand(request.UserName, request.Password);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = result.Error ?? "用户名或密码错误"
                };
            }

            var userDto = result.Value;

            // 更新连接管理器的用户信息
            _connectionManager.UpdateUserName(Context.ConnectionId, userDto.UserName);
            _connectionManager.SetUserId(Context.ConnectionId, userDto.Id);

            // 广播用户列表更新
            await BroadcastUserListAsync();

            // 转换为Shared.Models.User
            var user = new User
            {
                Id = userDto.Id,
                UserName = userDto.UserName,
                DisplayName = userDto.DisplayName
            };

            _logger.LogInformation("用户登录成功: {UserName} (ID: {UserId})", userDto.UserName, userDto.Id);

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
    /// 设置显示昵称 - 使用DDD Command
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

        // 从 Claims 中获取用户信息（支持 REST API 登录）
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            userId = _connectionManager.GetUserId(Context.ConnectionId);
        }
        
        if (string.IsNullOrEmpty(userId))
        {
            await Clients.Caller.SendAsync("DisplayNameResult", new { Success = false, Message = "请先登录" });
            return;
        }

        var command = new UpdateDisplayNameCommand(userId, displayName);
        var result = await _mediator.Send(command);

        if (result.IsSuccess)
        {
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
    /// 创建房间 - 使用 DDD MediatR Command
    /// </summary>
    public async Task<JoinRoomResponse> CreateRoom(CreateRoomRequest request)
    {
        // 使用统一的方法获取当前用户信息
        var (userId, userName) = GetCurrentUser();
        
        if (string.IsNullOrEmpty(userId))
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "请先登录"
            };
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 2)
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "房间名称至少2个字符"
            };
        }

        if (!request.IsPublic && string.IsNullOrWhiteSpace(request.Password))
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "私人房间必须设置密码"
            };
        }

        // 使用 MediatR 发送命令，遵循 DDD + CQRS 模式
        var command = new CreateRoomCommand(
            request.Name,
            request.Description,
            userId,
            request.IsPublic,
            request.IsPublic ? null : request.Password
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = result.Error
            };
        }

        var roomDto = result.Value;

        // 创建者加入房间分组
        await Groups.AddToGroupAsync(Context.ConnectionId, roomDto.Id);

        // 转换为 Shared.Models.ChatRoom 用于 SignalR 广播
        var chatRoom = new ChatRoom
        {
            Id = roomDto.Id,
            Name = roomDto.Name,
            Description = roomDto.Description,
            OwnerId = roomDto.OwnerId,
            IsPublic = roomDto.IsPublic,
            MemberCount = roomDto.MemberCount,
            CreatedAt = roomDto.CreatedAt
        };

        // 广播新房间创建
        await Clients.All.SendAsync("RoomCreated", new JoinRoomResponse
        {
            Success = true,
            Message = $"房间 '{roomDto.Name}' 创建成功",
            Room = chatRoom
        });

        _logger.LogInformation("房间创建成功: {RoomName} (ID: {RoomId}) by User {UserId}", 
            roomDto.Name, roomDto.Id, userId);

        return new JoinRoomResponse
        {
            Success = true,
            Message = $"房间 '{roomDto.Name}' 创建成功",
            Room = chatRoom
        };
    }

    /// <summary>
    /// 获取所有公共房间列表 - 使用DDD Query
    /// </summary>
    public async Task<List<ChatRoom>> GetRooms()
    {
        var roomDtos = await _mediator.Send(new GetPublicRoomsQuery());
        
        // 转换为 Shared.Models.ChatRoom 用于 SignalR 返回
        return roomDtos.Select(dto => new ChatRoom
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = dto.OwnerId,
            IsPublic = dto.IsPublic,
            MemberCount = dto.MemberCount,
            CreatedAt = dto.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// 获取我的房间列表 - 使用DDD Query
    /// </summary>
    public async Task<List<ChatRoom>> GetMyRooms()
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        if (string.IsNullOrEmpty(userId))
        {
            return new List<ChatRoom>();
        }

        var roomDtos = await _mediator.Send(new GetUserRoomsQuery(userId));
        
        // 转换为 Shared.Models.ChatRoom 用于 SignalR 返回
        return roomDtos.Select(dto => new ChatRoom
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = dto.OwnerId,
            IsPublic = dto.IsPublic,
            MemberCount = dto.MemberCount,
            CreatedAt = dto.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// 通过房间名称加入房间 - 使用DDD Command
    /// </summary>
    public async Task<JoinRoomResponse> JoinRoomByName(string roomName, string? password)
    {
        // 使用统一的方法获取当前用户信息
        var (userId, userName) = GetCurrentUser();

        if (string.IsNullOrEmpty(userId))
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "请先登录"
            };
        }

        if (string.IsNullOrWhiteSpace(roomName))
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "请输入房间名称"
            };
        }

        // 使用 MediatR 发送命令
        var command = new JoinRoomByNameCommand(userId, roomName, password);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = result.Error
            };
        }

        var roomDto = result.Value;

        // 加入 SignalR 分组
        await Groups.AddToGroupAsync(Context.ConnectionId, roomDto.Id);

        // 转换为 Shared.Models.ChatRoom 用于 SignalR 广播
        var chatRoom = new ChatRoom
        {
            Id = roomDto.Id,
            Name = roomDto.Name,
            Description = roomDto.Description,
            OwnerId = roomDto.OwnerId,
            IsPublic = roomDto.IsPublic,
            MemberCount = roomDto.MemberCount,
            CreatedAt = roomDto.CreatedAt
        };

        // 广播用户加入消息
        var joinMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName ?? "未知用户",
            RoomId = roomDto.Id,
            Message = $"欢迎 {userName ?? "未知用户"} 加入房间",
            Timestamp = DateTime.UtcNow
        };
        await Clients.Group(roomDto.Id).SendAsync("ReceiveMessage", joinMessage);

        // 广播房间用户列表更新
        await BroadcastRoomUserListAsync(roomDto.Id);

        _logger.LogInformation("用户 {UserName} (ID: {UserId}) 通过房间名称加入房间 {RoomName} (ID: {RoomId})", 
            userName, userId, roomDto.Name, roomDto.Id);

        return new JoinRoomResponse
        {
            Success = true,
            Message = $"成功加入房间 '{roomDto.Name}'",
            Room = chatRoom
        };
    }

    /// <summary>
    /// 搜索房间（根据名称模糊搜索）- 使用DDD Query
    /// </summary>
    public async Task<List<ChatRoom>> SearchRoomsByName(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            return new List<ChatRoom>();
        }

        var roomDtos = await _mediator.Send(new SearchRoomsQuery(roomName));
        
        // 转换为 Shared.Models.ChatRoom 用于 SignalR 返回
        return roomDtos.Select(dto => new ChatRoom
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = dto.OwnerId,
            IsPublic = dto.IsPublic,
            MemberCount = dto.MemberCount,
            CreatedAt = dto.CreatedAt
        }).ToList();
    }

    /// <summary>
    /// 加入房间 - 使用 DDD MediatR Command
    /// </summary>
    public async Task<JoinRoomResponse> JoinRoom(JoinRoomRequest request)
    {
        // 使用统一的方法获取当前用户信息
        var (userId, userName) = GetCurrentUser();

        if (string.IsNullOrEmpty(userId))
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = "请先登录"
            };
        }

        // 使用 MediatR 发送命令，遵循 DDD + CQRS 模式
        var command = new JoinRoomCommand(
            userId,
            request.RoomId,
            request.Password
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return new JoinRoomResponse
            {
                Success = false,
                Message = result.Error
            };
        }

        var roomDto = result.Value;

        // 加入 SignalR 分组
        await Groups.AddToGroupAsync(Context.ConnectionId, roomDto.Id);

        // 转换为 Shared.Models.ChatRoom 用于 SignalR 广播
        var chatRoom = new ChatRoom
        {
            Id = roomDto.Id,
            Name = roomDto.Name,
            Description = roomDto.Description,
            OwnerId = roomDto.OwnerId,
            IsPublic = roomDto.IsPublic,
            MemberCount = roomDto.MemberCount,
            CreatedAt = roomDto.CreatedAt
        };

        // 广播用户加入消息
        var joinMessage = new ChatMessage
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            UserName = userName ?? "未知用户",
            RoomId = roomDto.Id,
            Message = $"欢迎 {userName ?? "未知用户"} 加入房间",
            Timestamp = DateTime.UtcNow
        };
        await Clients.Group(roomDto.Id).SendAsync("ReceiveMessage", joinMessage);

        // 广播房间用户列表更新
        await BroadcastRoomUserListAsync(roomDto.Id);

        _logger.LogInformation("用户 {UserName} (ID: {UserId}) 加入房间 {RoomName} (ID: {RoomId})", 
            userName, userId, roomDto.Name, roomDto.Id);

        return new JoinRoomResponse
        {
            Success = true,
            Message = $"成功加入房间 '{roomDto.Name}'",
            Room = chatRoom
        };
    }

    /// <summary>
    /// 获取房间用户列表 - 使用DDD Query
    /// </summary>
    public async Task<List<string>> GetRoomUsers(string roomId)
    {
        return await _mediator.Send(new GetRoomUsersQuery(roomId));
    }

    /// <summary>
    /// 广播房间用户列表
    /// </summary>
    private async Task BroadcastRoomUserListAsync(string roomId)
    {
        var userNames = await GetRoomUsers(roomId);
        await Clients.Group(roomId).SendAsync("RoomUserListUpdated", userNames);
    }

    /// <summary>
    /// 离开房间 - 使用 DDD MediatR Command
    /// </summary>
    public async Task LeaveRoom(string roomId)
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        var userName = _connectionManager.GetUserName(Context.ConnectionId);

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // 使用 MediatR 发送命令，遵循 DDD + CQRS 模式
        var command = new LeaveRoomCommand(userId, roomId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("离开房间失败: {Error}", result.Error);
            return;
        }

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

        _logger.LogInformation("用户 {UserName} (ID: {UserId}) 离开房间 (ID: {RoomId})", 
            userName, userId, roomId);
    }

    /// <summary>
    /// 验证房间密码 - 使用DDD Query
    /// </summary>
    public async Task<bool> VerifyRoomPassword(string roomId, string password)
    {
        return await _mediator.Send(new VerifyRoomPasswordQuery(roomId, password ?? string.Empty));
    }

    #endregion

    #region 消息

    /// <summary>
    /// 发送房间消息 - 使用 DDD MediatR Command
    /// </summary>
    public async Task SendRoomMessage(SendMessageRequest request)
    {
        // 从 Claims 中获取用户信息（支持 REST API 登录）
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = Context.User?.FindFirstValue(ClaimTypes.Name) 
            ?? Context.User?.FindFirstValue("display_name");
        
        // 如果 Claims 中没有用户信息，尝试从 connectionManager 获取
        if (string.IsNullOrEmpty(userId))
        {
            userId = _connectionManager.GetUserId(Context.ConnectionId);
            userName = _connectionManager.GetUserName(Context.ConnectionId);
        }

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return;
        }

        const int maxMessageLength = 500;
        var messageContent = request.Message;
        if (messageContent.Length > maxMessageLength)
        {
            messageContent = messageContent[..maxMessageLength];
        }

        // 使用 MediatR 发送命令，遵循 DDD + CQRS 模式
        // 注意: SendMessageCommand 使用 string 类型，SendMessageRequest 使用 MessageType 枚举
        var messageTypeString = request.Type.ToString();
        
        var command = new SendMessageCommand(
            userId,
            userName ?? "未知用户",
            request.RoomId,
            messageContent,
            messageTypeString,
            request.MediaUrl,
            request.AltText
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", result.Error);
            return;
        }

        var messageDto = result.Value;

        // 转换为 Shared.Models.ChatMessage 用于 SignalR 广播
        var chatMessage = new ChatMessage
        {
            Id = messageDto.Id,
            UserId = messageDto.UserId,
            UserName = messageDto.UserName,
            RoomId = messageDto.RoomId,
            Message = messageDto.Content,
            Type = Enum.Parse<MessageType>(messageDto.Type),
            MediaUrl = messageDto.MediaUrl,
            AltText = messageDto.AltText,
            Timestamp = messageDto.Timestamp
        };

        // 广播给房间内所有用户
        await Clients.Group(request.RoomId).SendAsync("ReceiveMessage", chatMessage);

        _logger.LogDebug("房间消息已发送: {UserName} -> Room {RoomId}: {MessagePreview}", 
            userName, request.RoomId, messageContent[..Math.Min(50, messageContent.Length)]);
    }

    /// <summary>
    /// 获取房间消息历史 - 使用DDD Query
    /// </summary>
    public async Task<List<ChatMessage>> GetRoomMessages(string roomId, int count = 50)
    {
        var (userId, _) = GetCurrentUser();

        if (string.IsNullOrEmpty(userId))
        {
            return new List<ChatMessage>();
        }

        // 检查用户是否在房间中（公共房间"lobby"除外）
        if (roomId != "lobby")
        {
            var isInRoom = await _mediator.Send(new IsUserInRoomQuery(userId, roomId));
            if (!isInRoom)
            {
                await Clients.Caller.SendAsync("Error", "您不在该房间中，无法查看消息历史");
                return new List<ChatMessage>();
            }
        }

        var messageDtos = await _mediator.Send(new GetRoomMessagesQuery(roomId, count));
        
        // 转换为 Shared.Models.ChatMessage 用于 SignalR 返回
        return messageDtos.Select(dto => new ChatMessage
        {
            Id = dto.Id,
            UserId = dto.UserId,
            UserName = dto.UserName,
            RoomId = dto.RoomId,
            Message = dto.Content,
            Type = Enum.Parse<MessageType>(dto.Type),
            MediaUrl = dto.MediaUrl,
            AltText = dto.AltText,
            Timestamp = dto.Timestamp
        }).ToList();
    }

    #endregion

    #region 原有功能（使用DDD）

    /// <summary>
    /// 发送消息到所有客户端（全局消息）- 使用DDD Command
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

            // 获取发送者ID
            var userId = _connectionManager.GetUserId(Context.ConnectionId);
            if (string.IsNullOrEmpty(userId))
            {
                userId = "anonymous";
            }

            var command = new SendGlobalMessageCommand(
                userId,
                chatMessage.User,
                chatMessage.Message
            );

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("发送全局消息失败: {Error}", result.Error);
                return;
            }

            var messageDto = result.Value;

            // 转换为 Shared.Models.ChatMessage 用于 SignalR 广播
            var savedMessage = new ChatMessage
            {
                Id = messageDto.Id,
                UserId = messageDto.UserId,
                UserName = messageDto.UserName,
                RoomId = messageDto.RoomId,
                Message = messageDto.Content,
                Type = Enum.Parse<MessageType>(messageDto.Type),
                MediaUrl = messageDto.MediaUrl,
                AltText = messageDto.AltText,
                Timestamp = messageDto.Timestamp
            };
            
            await Clients.All.SendAsync("ReceiveMessage", savedMessage);
            
            _logger.LogDebug("全局消息已发送: {User} - {MessagePreview}", 
                messageDto.UserName, 
                messageDto.Content[..Math.Min(50, messageDto.Content.Length)]);
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
    /// 获取最近的消息历史 - 使用DDD Query
    /// </summary>
    public async Task GetRecentMessages(int count = 50)
    {
        var messageDtos = await _mediator.Send(new GetRecentMessagesQuery(count));
        
        // 转换为 Shared.Models.ChatMessage 用于 SignalR 返回
        var messages = messageDtos.Select(dto => new ChatMessage
        {
            Id = dto.Id,
            UserId = dto.UserId,
            UserName = dto.UserName,
            RoomId = dto.RoomId,
            Message = dto.Content,
            Type = Enum.Parse<MessageType>(dto.Type),
            MediaUrl = dto.MediaUrl,
            AltText = dto.AltText,
            Timestamp = dto.Timestamp
        }).ToList();
        
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
            // 首先检查 JWT Token 中是否有用户信息（REST API 登录方式）
            var userIdFromJwt = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userNameFromJwt = Context.User?.FindFirstValue(ClaimTypes.Name) 
                ?? Context.User?.FindFirstValue("display_name");
            
            if (!string.IsNullOrEmpty(userIdFromJwt) && !string.IsNullOrEmpty(userNameFromJwt))
            {
                // JWT 认证用户：使用 JWT 中的用户信息
                _connectionManager.AddConnection(Context.ConnectionId, userNameFromJwt);
                _connectionManager.SetUserId(Context.ConnectionId, userIdFromJwt);
                
                await Clients.Caller.SendAsync("SetDefaultUserName", userNameFromJwt);
                await BroadcastUserListAsync();
                await Clients.Others.SendAsync("UserJoined", userNameFromJwt);
                
                _logger.LogInformation("JWT认证用户已连接: {UserName} (ID: {UserId}, Connection: {ConnectionId})", 
                    userNameFromJwt, userIdFromJwt, Context.ConnectionId);
            }
            else
            {
                // 匿名用户：生成默认用户名
                var defaultUserName = $"User_{Guid.NewGuid().ToString()[..4]}";
                
                _connectionManager.AddConnection(Context.ConnectionId, defaultUserName);
                
                await Clients.Caller.SendAsync("SetDefaultUserName", defaultUserName);
                await BroadcastUserListAsync();
                await Clients.Others.SendAsync("UserJoined", defaultUserName);
                
                _logger.LogInformation("匿名用户已连接: {UserName} (Connection: {ConnectionId})", 
                    defaultUserName, Context.ConnectionId);
            }
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
            
            // 用户断开连接时，自动从所有加入的房间中移除
            if (!string.IsNullOrEmpty(userId))
            {
                await LeaveAllRoomsAsync(userId, userName);
            }
            
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

    /// <summary>
    /// 用户断开连接时，自动从所有房间中移除 - 使用DDD Query/Command
    /// </summary>
    private async Task LeaveAllRoomsAsync(string userId, string? userName)
    {
        try
        {
            // 使用 MediatR 获取用户加入的所有房间 (DDD Query)
            var userRooms = await _mediator.Send(new GetUserRoomsQuery(userId));
            
            foreach (var room in userRooms)
            {
                // 使用 MediatR 从房间移除用户 (DDD Command)
                await _mediator.Send(new LeaveRoomCommand(userId, room.Id));
                
                // 离开 SignalR 分组
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id);
                
                // 广播用户离开消息
                var leaveMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    UserName = userName ?? "未知用户",
                    RoomId = room.Id,
                    Message = $"{userName ?? "未知用户"} 离开了房间",
                    Timestamp = DateTime.UtcNow
                };
                await Clients.Group(room.Id).SendAsync("ReceiveMessage", leaveMessage);
                
                _logger.LogInformation("用户断开连接，自动离开房间: {UserName} -> {RoomName}", 
                    userName, room.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户断开连接时清理房间成员失败: {UserId}", userId);
        }
    }

    #endregion

    /// <summary>
    /// 获取在线用户列表
    /// </summary>
    public async Task GetOnlineUsers()
    {
        var connections = _connectionManager.GetAllConnections();
        var userNames = connections.Select(c => c.UserName).ToList();
        await Clients.Caller.SendAsync("UpdateUserList", userNames);
    }

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
