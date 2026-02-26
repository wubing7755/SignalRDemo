namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 连接信息
/// </summary>
public class ConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}

/// <summary>
/// 用户连接管理接口
/// </summary>
public interface IUserConnectionManager
{
    // 连接管理
    void AddConnection(string connectionId, string userName);
    void RemoveConnection(string connectionId);
    
    // 用户ID管理
    void SetUserId(string connectionId, string userId);
    string? GetUserId(string connectionId);
    void ClearUserId(string connectionId);
    
    // 用户名管理
    void UpdateUserName(string connectionId, string userName);
    string? GetUserName(string connectionId);
    
    // 查询
    List<string> GetUserConnections(string userId);
    string? GetConnectionUser(string connectionId);
    int GetOnlineUserCount();
    List<string> GetOnlineUserIds();
    bool IsUserOnline(string userId);
    List<ConnectionInfo> GetAllConnections();
}
