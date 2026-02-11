using SignalRDemo.Shared.Models;

namespace SignalRDemo.Server.Services;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 注册新用户
    /// </summary>
    Task<User?> RegisterAsync(string userName, string password, string? displayName);

    /// <summary>
    /// 用户登录
    /// </summary>
    Task<User?> LoginAsync(string userName, string password);

    /// <summary>
    /// 根据ID获取用户
    /// </summary>
    Task<User?> GetUserByIdAsync(string userId);

    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<User?> GetUserByUserNameAsync(string userName);

    /// <summary>
    /// 更新最后登录时间
    /// </summary>
    Task UpdateLastLoginAsync(string userId);
}
