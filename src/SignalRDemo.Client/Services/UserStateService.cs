using Microsoft.JSInterop;
using SignalRDemo.Client.Services.Abstractions;
using SignalRDemo.Shared.Models;
using System.Text.Json;

namespace SignalRDemo.Client.Services;

/// <summary>
/// 用户状态服务实现 - 管理用户登录状态和持久化
/// </summary>
public class UserStateService : IUserStateService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<UserStateService> _logger;
    private User? _currentUser;

    private const string UserStorageKey = "signalchat_user";

    public User? CurrentUser => _currentUser;
    public string CurrentUserId => _currentUser?.Id ?? string.Empty;
    public string CurrentUserName => _currentUser?.UserName ?? string.Empty;
    public bool IsLoggedIn => _currentUser != null;

    public event Action<User?>? AuthStateChanged;

    public UserStateService(IJSRuntime jsRuntime, ILogger<UserStateService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserStorageKey);
            if (!string.IsNullOrEmpty(userJson))
            {
                _currentUser = JsonSerializer.Deserialize<User>(userJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                if (_currentUser != null)
                {
                    _logger.LogInformation("从存储恢复用户状态: {UserName}", _currentUser.UserName);
                    AuthStateChanged?.Invoke(_currentUser);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化用户状态失败");
        }
    }

    public async Task SetCurrentUserAsync(User user)
    {
        _currentUser = user;
        
        try
        {
            var userJson = JsonSerializer.Serialize(user);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserStorageKey, userJson);
            _logger.LogInformation("用户状态已保存: {UserName}", user.UserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存用户状态失败");
        }
        
        AuthStateChanged?.Invoke(user);
    }

    public async Task ClearCurrentUserAsync()
    {
        _currentUser = null;
        
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserStorageKey);
            _logger.LogInformation("用户状态已清除");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清除用户状态失败");
        }
        
        AuthStateChanged?.Invoke(null);
    }

    public async Task UpdateDisplayNameAsync(string displayName)
    {
        if (_currentUser == null) return;

        _currentUser.DisplayName = displayName;
        
        try
        {
            var userJson = JsonSerializer.Serialize(_currentUser);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserStorageKey, userJson);
            _logger.LogInformation("用户显示名称已更新: {DisplayName}", displayName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新显示名称失败");
        }
        
        AuthStateChanged?.Invoke(_currentUser);
    }
}
