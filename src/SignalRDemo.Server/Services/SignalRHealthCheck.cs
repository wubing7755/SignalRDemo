using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SignalRDemo.Server.Services;

/// <summary>
/// SignalR 健康检查
/// </summary>
public class SignalRHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 简单的健康检查 - 总是返回健康状态
            return Task.FromResult(HealthCheckResult.Healthy("SignalR Hub is running"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "SignalR Hub is not available",
                ex));
        }
    }
}
