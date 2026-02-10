namespace SignalRDemo.Client.Services;

/// <summary>
/// 防抖服务 - 防止频繁触发操作
/// </summary>
public class DebounceService : IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly object _lock = new();

    /// <summary>
    /// 执行防抖操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="delayMilliseconds">延迟毫秒数</param>
    public void Debounce(Action action, int delayMilliseconds = 500)
    {
        lock (_lock)
        {
            // 取消之前的任务
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            var token = _cancellationTokenSource.Token;

            Task.Delay(delayMilliseconds, token)
                .ContinueWith(task =>
                {
                    if (!task.IsCanceled && !token.IsCancellationRequested)
                    {
                        action();
                    }
                }, TaskScheduler.Default);
        }
    }

    /// <summary>
    /// 执行防抖异步操作
    /// </summary>
    public async Task DebounceAsync(Func<Task> action, int delayMilliseconds = 500)
    {
        CancellationTokenSource? cts;
        
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            cts = _cancellationTokenSource;
        }

        try
        {
            await Task.Delay(delayMilliseconds, cts.Token);
            await action();
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不处理
        }
    }

    /// <summary>
    /// 立即执行并取消待处理的防抖
    /// </summary>
    public void Flush()
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
    }
}

/// <summary>
/// 节流服务 - 限制操作执行频率
/// </summary>
public class ThrottleService : IDisposable
{
    private DateTime _lastExecutionTime = DateTime.MinValue;
    private readonly object _lock = new();

    /// <summary>
    /// 执行节流操作
    /// </summary>
    public void Throttle(Action action, int intervalMilliseconds = 1000)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastExecutionTime).TotalMilliseconds;

            if (elapsed >= intervalMilliseconds)
            {
                _lastExecutionTime = now;
                action();
            }
        }
    }

    public void Dispose()
    {
        // 无需特殊清理
    }
}
