using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SignalRDemo.Server.Services;

/// <summary>
/// SignalR Hub 健康检查
/// </summary>
public class SignalRHealthCheck : IHealthCheck
{
    private readonly IUserConnectionManager _connectionManager;
    private readonly ILogger<SignalRHealthCheck> _logger;

    public SignalRHealthCheck(
        IUserConnectionManager connectionManager,
        ILogger<SignalRHealthCheck> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionCount = _connectionManager.GetConnectionCount();
            
            // 记录当前连接数作为诊断数据
            var data = new Dictionary<string, object>
            {
                { "ActiveConnections", connectionCount },
                { "CheckedAt", DateTime.UtcNow }
            };

            // 可以根据实际需求调整健康判断逻辑
            // 这里仅作为示例，始终返回健康状态
            return Task.FromResult(HealthCheckResult.Healthy(
                $"SignalR Hub 运行正常，当前连接数: {connectionCount}",
                data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR 健康检查失败");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "SignalR Hub 健康检查失败",
                ex));
        }
    }
}
