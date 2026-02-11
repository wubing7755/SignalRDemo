using System.Collections.Concurrent;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 用户连接管理器接口
/// </summary>
public interface IUserConnectionManager
{
    void AddConnection(string connectionId, string userName);
    void RemoveConnection(string connectionId);
    string? GetUserName(string connectionId);
    IReadOnlyList<UserConnection> GetAllConnections();
    int GetConnectionCount();
    bool IsUserOnline(string userName);
    void UpdateUserName(string connectionId, string newUserName);
    void SetUserId(string connectionId, string userId);
    string? GetUserId(string connectionId);
    void ClearUserId(string connectionId);
}

/// <summary>
/// 用户连接管理器实现
/// </summary>
public class UserConnectionManager : IUserConnectionManager
{
    private readonly ConcurrentDictionary<string, UserConnection> _connections = new();

    public void AddConnection(string connectionId, string userName)
    {
        _connections[connectionId] = new UserConnection
        {
            ConnectionId = connectionId,
            UserName = userName,
            ConnectedAt = DateTime.UtcNow
        };
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

    public void SetUserId(string connectionId, string userId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.UserId = userId;
        }
    }

    public string? GetUserId(string connectionId)
    {
        return _connections.TryGetValue(connectionId, out var connection) ? connection.UserId : null;
    }

    public void ClearUserId(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.UserId = null;
        }
    }
}
