using System.Net.Http.Json;
using Microsoft.JSInterop;
using SignalRDemo.Shared.Models;

namespace SignalRDemo.Client.Services;

/// <summary>
/// 认证服务 - 处理用户登录、注册和 Token 管理
/// </summary>
public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<AuthService> _logger;
    private const string AuthEndpoint = "/api/auth";

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<LoginResponse> LoginAsync(string userName, string password)
    {
        try
        {
            var request = new LoginRequest
            {
                UserName = userName,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync($"{AuthEndpoint}/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.Success == true && result.Token != null)
                {
                    await StoreTokenAsync(result.Token);
                    await StoreRefreshTokenAsync(result.RefreshToken);
                }
                return result ?? new LoginResponse { Success = false, Message = "登录失败" };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new LoginResponse { Success = false, Message = errorContent ?? "登录失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败");
            return new LoginResponse { Success = false, Message = "登录失败，请稍后重试" };
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    public async Task<LoginResponse> RegisterAsync(string userName, string password, string? displayName)
    {
        try
        {
            var request = new RegisterRequest
            {
                UserName = userName,
                Password = password,
                DisplayName = displayName
            };

            var response = await _httpClient.PostAsJsonAsync($"{AuthEndpoint}/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.Success == true && result.Token != null)
                {
                    await StoreTokenAsync(result.Token);
                    await StoreRefreshTokenAsync(result.RefreshToken);
                }
                return result ?? new LoginResponse { Success = false, Message = "注册失败" };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new LoginResponse { Success = false, Message = errorContent ?? "注册失败" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "注册失败");
            return new LoginResponse { Success = false, Message = "注册失败，请稍后重试" };
        }
    }

    /// <summary>
    /// 刷新 Token
    /// </summary>
    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var response = await _httpClient.PostAsJsonAsync($"{AuthEndpoint}/refresh", new { RefreshToken = refreshToken });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result?.Success == true && result.Token != null)
                {
                    await StoreTokenAsync(result.Token);
                    await StoreRefreshTokenAsync(result.RefreshToken);
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新 Token 失败");
            return false;
        }
    }

    /// <summary>
    /// 退出登录
    /// </summary>
    public async Task LogoutAsync()
    {
        try
        {
            await _httpClient.PostAsync($"{AuthEndpoint}/logout", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "退出登录失败");
        }
        finally
        {
            await ClearTokensAsync();
        }
    }

    /// <summary>
    /// 检查是否已登录
    /// </summary>
    public async Task<bool> IsLoggedInAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// 获取当前 Token
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "AccessToken");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 获取刷新 Token
    /// </summary>
    private async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "RefreshToken");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 存储 Token
    /// </summary>
    private async Task StoreTokenAsync(string? token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "AccessToken", token);
        }
    }

    /// <summary>
    /// 存储刷新 Token
    /// </summary>
    private async Task StoreRefreshTokenAsync(string? refreshToken)
    {
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "RefreshToken", refreshToken);
        }
    }

    /// <summary>
    /// 清除 Token
    /// </summary>
    private async Task ClearTokensAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "AccessToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "RefreshToken");
    }

    /// <summary>
    /// 将 Token 添加到 HttpClient 默认请求头
    /// </summary>
    public async Task AddTokenToClientAsync()
    {
        var token = await GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}
