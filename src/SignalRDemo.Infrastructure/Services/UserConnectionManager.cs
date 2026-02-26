namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 用户连接管理器实现
/// </summary>
public class UserConnectionManager : IUserConnectionManager
{
    private readonly Dictionary<string, ConnectionInfo> _connections = new(); // connectionId -> ConnectionInfo
    private readonly Dictionary<string, List<string>> _userConnections = new(); // userId -> connectionIds
    private readonly ReaderWriterLockSlim _lock = new();

    public void AddConnection(string connectionId, string userName)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_connections.ContainsKey(connectionId))
            {
                _connections[connectionId] = new ConnectionInfo
                {
                    ConnectionId = connectionId,
                    UserName = userName
                };
            }
            else
            {
                _connections[connectionId].UserName = userName;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void RemoveConnection(string connectionId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_connections.TryGetValue(connectionId, out var info))
            {
                var userId = info.UserId;
                _connections.Remove(connectionId);

                if (!string.IsNullOrEmpty(userId) && _userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].Remove(connectionId);
                    if (_userConnections[userId].Count == 0)
                    {
                        _userConnections.Remove(userId);
                    }
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void SetUserId(string connectionId, string userId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_connections.ContainsKey(connectionId))
            {
                _connections[connectionId] = new ConnectionInfo
                {
                    ConnectionId = connectionId,
                    UserId = userId
                };
            }
            else
            {
                // 移除旧的用户ID关联
                var oldUserId = _connections[connectionId].UserId;
                if (!string.IsNullOrEmpty(oldUserId) && _userConnections.ContainsKey(oldUserId))
                {
                    _userConnections[oldUserId].Remove(connectionId);
                    if (_userConnections[oldUserId].Count == 0)
                    {
                        _userConnections.Remove(oldUserId);
                    }
                }

                _connections[connectionId].UserId = userId;

                // 添加新的用户ID关联
                if (!_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId] = new List<string>();
                }
                if (!_userConnections[userId].Contains(connectionId))
                {
                    _userConnections[userId].Add(connectionId);
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public string? GetUserId(string connectionId)
    {
        _lock.EnterReadLock();
        try
        {
            return _connections.TryGetValue(connectionId, out var info) ? info.UserId : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void ClearUserId(string connectionId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_connections.TryGetValue(connectionId, out var info))
            {
                var userId = info.UserId;
                info.UserId = string.Empty;

                if (!string.IsNullOrEmpty(userId) && _userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].Remove(connectionId);
                    if (_userConnections[userId].Count == 0)
                    {
                        _userConnections.Remove(userId);
                    }
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void UpdateUserName(string connectionId, string userName)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_connections.ContainsKey(connectionId))
            {
                _connections[connectionId].UserName = userName;
            }
            else
            {
                _connections[connectionId] = new ConnectionInfo
                {
                    ConnectionId = connectionId,
                    UserName = userName
                };
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public string? GetUserName(string connectionId)
    {
        _lock.EnterReadLock();
        try
        {
            return _connections.TryGetValue(connectionId, out var info) ? info.UserName : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public List<string> GetUserConnections(string userId)
    {
        _lock.EnterReadLock();
        try
        {
            return _userConnections.TryGetValue(userId, out var connections)
                ? connections.ToList()
                : new List<string>();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public string? GetConnectionUser(string connectionId)
    {
        _lock.EnterReadLock();
        try
        {
            return _connections.TryGetValue(connectionId, out var info) ? info.UserId : null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public int GetOnlineUserCount()
    {
        _lock.EnterReadLock();
        try
        {
            return _userConnections.Count;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public List<string> GetOnlineUserIds()
    {
        _lock.EnterReadLock();
        try
        {
            return _userConnections.Keys.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public bool IsUserOnline(string userId)
    {
        _lock.EnterReadLock();
        try
        {
            return _userConnections.ContainsKey(userId) && _userConnections[userId].Count > 0;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public List<ConnectionInfo> GetAllConnections()
    {
        _lock.EnterReadLock();
        try
        {
            return _connections.Values.ToList();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
