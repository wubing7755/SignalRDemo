using System.Security.Cryptography;
using System.Text;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 基于 JSON 文件的用户服务实现
/// </summary>
public class JsonUserService : IUserService
{
    private readonly string _dataPath;
    private readonly string _filePath;
    private List<User> _users = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<JsonUserService> _logger;
    private readonly object _saveLock = new();

    public JsonUserService(ILogger<JsonUserService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _dataPath = Path.Combine(env.ContentRootPath, "Data");
        _filePath = Path.Combine(_dataPath, "users.json");
        
        EnsureDataDirectory();
        LoadUsers();
    }

    private void EnsureDataDirectory()
    {
        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
            _logger.LogInformation("创建数据目录: {Path}", _dataPath);
        }
    }

    private void LoadUsers()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var users = System.Text.Json.JsonSerializer.Deserialize<List<User>>(json);
                if (users != null)
                {
                    _users = users;
                    _logger.LogInformation("已加载 {Count} 个用户", _users.Count);
                }
            }
            else
            {
                _logger.LogInformation("用户数据文件不存在，将创建新文件");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载用户数据失败");
            _users = new List<User>();
        }
    }

    private void SaveUsers()
    {
        lock (_saveLock)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_users, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存用户数据失败");
            }
        }
    }

    public Task<User?> RegisterAsync(string userName, string password, string? displayName)
    {
        _lock.EnterWriteLock();
        try
        {
            // 检查用户名是否已存在
            if (_users.Any(u => u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("注册失败：用户名 {UserName} 已存在", userName);
                return Task.FromResult<User?>(null);
            }

            var user = new User
            {
                UserName = userName,
                DisplayName = displayName ?? userName,
                PasswordHash = HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            _users.Add(user);
            SaveUsers();
            _logger.LogInformation("用户 {UserName} 注册成功", userName);

            // 返回不包含密码哈希的用户信息
            var safeUser = new User
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt
            };

            return Task.FromResult<User?>(safeUser);
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
                u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

            if (user == null || !VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("登录失败：用户名或密码错误 {UserName}", userName);
                return Task.FromResult<User?>(null);
            }

            // 更新最后登录时间
            user.LastLoginAt = DateTime.UtcNow;
            SaveUsers();

            _logger.LogInformation("用户 {UserName} 登录成功", userName);

            // 返回不包含密码哈希的用户信息
            var safeUser = new User
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Task.FromResult<User?>(safeUser);
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
            if (user == null)
            {
                return Task.FromResult<User?>(null);
            }

            var safeUser = new User
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Task.FromResult<User?>(safeUser);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<User?> GetUserByUserNameAsync(string userName)
    {
        _lock.EnterReadLock();
        try
        {
            var user = _users.FirstOrDefault(u => 
                u.UserName.Equals(userName, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                return Task.FromResult<User?>(null);
            }

            var safeUser = new User
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return Task.FromResult<User?>(safeUser);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task UpdateLastLoginAsync(string userId)
    {
        _lock.EnterWriteLock();
        try
        {
            var user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                SaveUsers();
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 使用 PBKDF2 哈希密码
    /// </summary>
    private static string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var salt = new byte[16];
        rng.GetBytes(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);

        var saltHex = Convert.ToHexString(salt);
        var hashHex = Convert.ToHexString(hash);

        return $"{saltHex}:{hashHex}";
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    private static bool VerifyPassword(string password, string storedHash)
    {
        try
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromHexString(parts[0]);
            var storedHashBytes = Convert.FromHexString(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
            var computedHash = pbkdf2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHashBytes);
        }
        catch
        {
            return false;
        }
    }
}
