using System.Collections.Concurrent;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 用户连接管理器接口
/// </summary>
public interface IUserConnectionManager
{
    /// <summary>
    /// 添加用户连接
    /// </summary>
    void AddConnection(string connectionId, string userName);
    
    /// <summary>
    /// 移除用户连接
    /// </summary>
    void RemoveConnection(string connectionId);
    
    /// <summary>
    /// 根据连接ID获取用户名
    /// </summary>
    string? GetUserName(string connectionId);
    
    /// <summary>
    /// 获取所有在线用户列表
    /// </summary>
    IReadOnlyList<UserConnection> GetAllConnections();
    
    /// <summary>
    /// 获取在线用户数量
    /// </summary>
    int GetConnectionCount();
    
    /// <summary>
    /// 检查用户是否在线
    /// </summary>
    bool IsUserOnline(string userName);
    
    /// <summary>
    /// 更新用户名
    /// </summary>
    void UpdateUserName(string connectionId, string newUserName);
}

/// <summary>
/// 用户连接管理器实现
/// </summary>
public class UserConnectionManager : IUserConnectionManager
{
    // 使用 ConcurrentDictionary 保证线程安全
    private readonly ConcurrentDictionary<string, UserConnection> _connections = new();

    public void AddConnection(string connectionId, string userName)
    {
        var connection = new UserConnection
        {
            ConnectionId = connectionId,
            UserName = userName,
            ConnectedAt = DateTime.UtcNow
        };
        
        _connections[connectionId] = connection;
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public string? GetUserName(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var connection) ? connection.UserName : null;
    }

    public IReadOnlyList<UserConnection> GetAllConnections()
    {
        return _connections.Values.OrderBy(c => c.UserName).ToList().AsReadOnly();
    }

    public int GetConnectionCount()
    {
        return _connections.Count;
    }

    public bool IsUserOnline(string userName)
    {
        return _connections.Values.Any(c => 
            c.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
    }

    public void UpdateUserName(string connectionId, string newUserName)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.UserName = newUserName;
        }
    }
}
