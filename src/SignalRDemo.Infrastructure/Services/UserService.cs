using SignalRDemo.Shared.Models;
using System.Security.Cryptography;
using System.Text;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : IUserService
{
    private readonly List<User> _users = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public UserService()
    {
        // 添加默认用户
        _users.Add(new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "admin",
            DisplayName = "管理员",
            PasswordHash = HashPassword("admin123"),
            CreatedAt = DateTime.UtcNow
        });
    }

    public Task<User?> RegisterAsync(string userName, string password, string? displayName = null)
    {
        _lock.EnterWriteLock();
        try
        {
            // 检查用户名是否已存在
            if (_users.Any(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult<User?>(null);
            }

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = userName,
                DisplayName = displayName ?? userName,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            _users.Add(user);
            return Task.FromResult<User?>(user);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public Task<User?> LoginAsync(string userName, string password)
    {
        _lock.EnterReadLock();
        try
        {
            var user = _users.FirstOrDefault(u => 
                u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase) &&
                u.PasswordHash == HashPassword(password));

            return Task.FromResult<User?>(user);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<User?> GetUserByIdAsync(string userId)
    {
        _lock.EnterReadLock();
        try
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            return Task.FromResult<User?>(user);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<User?> GetUserByNameAsync(string userName)
    {
        _lock.EnterReadLock();
        try
        {
            var user = _users.FirstOrDefault(u => 
                u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<User?>(user);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<User?> GetUserByUserNameAsync(string userName)
    {
        return GetUserByNameAsync(userName);
    }

    public Task<bool> UpdateUserAsync(User user)
    {
        _lock.EnterWriteLock();
        try
        {
            var existing = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existing == null)
                return Task.FromResult(false);

            existing.DisplayName = user.DisplayName;
            if (!string.IsNullOrEmpty(user.PasswordHash))
                existing.PasswordHash = user.PasswordHash;

            return Task.FromResult(true);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
