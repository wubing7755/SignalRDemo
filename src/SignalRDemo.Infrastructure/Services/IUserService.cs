using SignalRDemo.Shared.Models;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    Task<User?> RegisterAsync(string userName, string password, string? displayName = null);
    Task<User?> LoginAsync(string userName, string password);
    Task<User?> GetUserByIdAsync(string userId);
    Task<User?> GetUserByNameAsync(string userName);
    Task<User?> GetUserByUserNameAsync(string userName);
    Task<bool> UpdateUserAsync(User user);
}
