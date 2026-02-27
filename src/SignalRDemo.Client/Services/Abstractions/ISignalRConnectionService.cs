using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRDemo.Client.Services.Abstractions;

/// <summary>
/// SignalR 连接服务接口 - 负责纯粹的连接管理
/// </summary>
public interface ISignalRConnectionService : IAsyncDisposable
{
    /// <summary>
    /// 当前连接状态
    /// </summary>
    HubConnectionState State { get; }
    
    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    event Action<HubConnectionState>? StateChanged;
    
    /// <summary>
    /// 连接关闭事件
    /// </summary>
    event Action<Exception?>? Closed;
    
    /// <summary>
    /// 重新连接成功事件
    /// </summary>
    event Action<string>? Reconnected;
    
    /// <summary>
    /// 初始化连接
    /// </summary>
    Task InitializeAsync(string hubUrl, string? accessToken = null);
    
    /// <summary>
    /// 重新连接
    /// </summary>
    Task ReconnectAsync();
    
    /// <summary>
    /// 调用 Hub 方法（无返回值）
    /// </summary>
    Task InvokeAsync(string methodName, params object?[] args);
    
    /// <summary>
    /// 调用 Hub 方法（有返回值）
    /// </summary>
    Task<T?> InvokeAsync<T>(string methodName, params object?[] args);
    
    /// <summary>
    /// 注册事件处理器
    /// </summary>
    void On<T>(string methodName, Action<T> handler);
    
    /// <summary>
    /// 注册事件处理器（无参数）
    /// </summary>
    void On(string methodName, Action handler);
    
    /// <summary>
    /// 移除事件处理器
    /// </summary>
    void Remove(string methodName);
}
