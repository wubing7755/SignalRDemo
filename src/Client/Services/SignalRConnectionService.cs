using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using SignalRDemo.Client.Services.Abstractions;

namespace SignalRDemo.Client.Services;

/// <summary>
/// SignalR 连接服务实现 - 纯粹的连接管理，不包含业务逻辑
/// </summary>
public class SignalRConnectionService : ISignalRConnectionService
{
    private HubConnection? _hubConnection;
    private string? _hubUrl;
    private string? _accessToken;
    private readonly ILogger<SignalRConnectionService> _logger;

    public HubConnectionState State => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public event Action<HubConnectionState>? StateChanged;
    public event Action<Exception?>? Closed;
    public event Action<string>? Reconnected;

    public SignalRConnectionService(ILogger<SignalRConnectionService> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(string hubUrl, string? accessToken = null)
    {
        _hubUrl = hubUrl;
        _accessToken = accessToken;

        if (_hubConnection != null)
        {
            await DisposeAsync();
        }

        try
        {
            var builder = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        options.AccessTokenProvider = () => Task.FromResult(accessToken)!;
                    }
                })
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .AddMessagePackProtocol();

            _hubConnection = builder.Build();

            // 连接状态事件
            _hubConnection.Reconnecting += error =>
            {
                _logger.LogWarning("SignalR 正在重连...");
                StateChanged?.Invoke(HubConnectionState.Reconnecting);
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += connectionId =>
            {
                _logger.LogInformation("SignalR 重连成功: {ConnectionId}", connectionId);
                StateChanged?.Invoke(HubConnectionState.Connected);
                Reconnected?.Invoke(connectionId ?? string.Empty);
                return Task.CompletedTask;
            };

            _hubConnection.Closed += error =>
            {
                _logger.LogInformation("SignalR 连接已关闭");
                StateChanged?.Invoke(HubConnectionState.Disconnected);
                Closed?.Invoke(error);
                return Task.CompletedTask;
            };

            await _hubConnection.StartAsync();
            StateChanged?.Invoke(HubConnectionState.Connected);
            _logger.LogInformation("SignalR 连接已建立: {HubUrl}", hubUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR 连接失败");
            StateChanged?.Invoke(HubConnectionState.Disconnected);
            throw;
        }
    }

    public async Task ReconnectAsync()
    {
        if (string.IsNullOrEmpty(_hubUrl))
        {
            throw new InvalidOperationException("Hub URL 未设置");
        }

        await InitializeAsync(_hubUrl, _accessToken);
    }

    public async Task InvokeAsync(string methodName, params object?[] args)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("SignalR 未连接");
        }

        try
        {
            await _hubConnection.SendCoreAsync(methodName, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用 Hub 方法失败: {Method}", methodName);
            throw;
        }
    }

    public async Task<T?> InvokeAsync<T>(string methodName, params object?[] args)
    {
        if (_hubConnection?.State != HubConnectionState.Connected)
        {
            throw new InvalidOperationException("SignalR 未连接");
        }

        try
        {
            return await _hubConnection.InvokeCoreAsync<T>(methodName, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "调用 Hub 方法失败: {Method}", methodName);
            throw;
        }
    }

    public void On<T>(string methodName, Action<T> handler)
    {
        _hubConnection?.On(methodName, handler);
    }

    public void On(string methodName, Action handler)
    {
        _hubConnection?.On(methodName, handler);
    }

    public void Remove(string methodName)
    {
        _hubConnection?.Remove(methodName);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "释放 SignalR 连接时发生错误");
            }
            finally
            {
                _hubConnection = null;
                StateChanged?.Invoke(HubConnectionState.Disconnected);
            }
        }
    }
}
