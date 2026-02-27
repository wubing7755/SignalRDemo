using SignalRDemo.Shared.Models;

namespace SignalRDemo.Client.Services.Abstractions;

/// <summary>
/// 用户状态服务接口 - 管理用户登录状态和持久化
/// </summary>
public interface IUserStateService
{
    /// <summary>
    /// 当前用户信息
    /// </summary>
    User? CurrentUser { get; }
    
    /// <summary>
    /// 当前用户ID
    /// </summary>
    string CurrentUserId { get; }
    
    /// <summary>
    /// 当前用户名
    /// </summary>
    string CurrentUserName { get; }
    
    /// <summary>
    /// 是否已登录
    /// </summary>
    bool IsLoggedIn { get; }
    
    /// <summary>
    /// 认证状态变化事件
    /// </summary>
    event Action<User?>? AuthStateChanged;
    
    /// <summary>
    /// 初始化服务（从持久化存储恢复状态）
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// 设置当前用户（登录成功后调用）
    /// </summary>
    Task SetCurrentUserAsync(User user);
    
    /// <summary>
    /// 清除当前用户（退出登录时调用）
    /// </summary>
    Task ClearCurrentUserAsync();
    
    /// <summary>
    /// 更新显示昵称
    /// </summary>
    Task UpdateDisplayNameAsync(string displayName);
}
