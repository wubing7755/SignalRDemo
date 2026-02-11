using Microsoft.AspNetCore.SignalR;
using SignalRDemo.Server.Services;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Hubs;

/// <summary>
/// 聊天 Hub - 处理实时消息和用户连接
/// </summary>
public class ChatHub : Hub
{
    private readonly IChatRepository _chatRepository;
    private readonly IUserConnectionManager _connectionManager;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatRepository chatRepository,
        IUserConnectionManager connectionManager,
        ILogger<ChatHub> logger)
    {
        _chatRepository = chatRepository;
        _connectionManager = connectionManager;
        _logger = logger;
    }

    /// <summary>
    /// 发送消息到所有客户端
    /// </summary>
    public async Task SendMessage(ChatMessage chatMessage)
    {
        try
        {
            // 验证消息
            if (string.IsNullOrWhiteSpace(chatMessage.Message))
            {
                return;
            }

            // 限制消息长度
            const int maxMessageLength = 500;
            if (chatMessage.Message.Length > maxMessageLength)
            {
                chatMessage.Message = chatMessage.Message[..maxMessageLength];
            }

            // 确保时间戳由服务器设置（防止客户端篡改）
            chatMessage.Timestamp = DateTime.UtcNow;
            
            // 保存消息到仓库
            await _chatRepository.SaveMessageAsync(chatMessage);
            
            // 广播消息
            await Clients.All.SendAsync("ReceiveMessage", chatMessage);
            
            _logger.LogDebug("消息已发送: {User} - {MessagePreview}", 
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

        // 限制用户名长度
        const int maxUserNameLength = 20;
        if (userName.Length > maxUserNameLength)
        {
            userName = userName[..maxUserNameLength];
        }

        _connectionManager.UpdateUserName(Context.ConnectionId, userName);
        
        // 通知所有客户端更新用户列表
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

    /// <summary>
    /// 客户端连接时
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        try
        {
            // 生成默认用户名
            var defaultUserName = $"User_{Guid.NewGuid().ToString()[..4]}";
            
            // 添加到连接管理器
            _connectionManager.AddConnection(Context.ConnectionId, defaultUserName);
            
            // 发送默认用户名给客户端
            await Clients.Caller.SendAsync("SetDefaultUserName", defaultUserName);
            
            // 广播用户列表更新
            await BroadcastUserListAsync();
            
            // 广播有新用户加入（使用用户名而不是 connectionId）
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
            // 获取断开连接的用户名
            var userName = _connectionManager.GetUserName(Context.ConnectionId);
            
            // 从连接管理器移除
            _connectionManager.RemoveConnection(Context.ConnectionId);
            
            // 广播用户列表更新
            await BroadcastUserListAsync();
            
            // 广播用户离开（使用用户名）
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
    /// 广播在线用户列表
    /// </summary>
    private async Task BroadcastUserListAsync()
    {
        var connections = _connectionManager.GetAllConnections();
        var userNames = connections.Select(c => c.UserName).ToList();
        await Clients.All.SendAsync("UpdateUserList", userNames);
    }
}
