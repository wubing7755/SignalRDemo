namespace SignalRDemo.Client.Services;

/// <summary>
/// 防抖服务 - 防止频繁触发操作
/// </summary>
public class DebounceService : IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly object _lock = new();
    private bool _disposed;

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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
        }

        _disposed = true;
    }

    ~DebounceService()
    {
        Dispose(false);
    }
}

/// <summary>
/// 节流服务 - 限制操作执行频率
/// </summary>
public class ThrottleService : IDisposable
{
    private DateTime _lastExecutionTime = DateTime.MinValue;
    private readonly object _lock = new();
    private bool _disposed;
    private const int DefaultInterval = 1000;

    /// <summary>
    /// 执行节流操作
    /// </summary>
    /// <param name="action">要执行的操作</param>
    /// <param name="intervalMilliseconds">最小间隔时间（毫秒）</param>
    /// <returns>如果执行了操作返回 true，否则返回 false</returns>
    public bool Throttle(Action action, int intervalMilliseconds = DefaultInterval)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastExecutionTime).TotalMilliseconds;

            if (elapsed >= intervalMilliseconds)
            {
                _lastExecutionTime = now;
                action();
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 检查是否允许执行（不执行操作）
    /// </summary>
    /// <param name="intervalMilliseconds">最小间隔时间（毫秒）</param>
    /// <returns>如果允许执行返回 true，否则返回 false</returns>
    public bool CanExecute(int intervalMilliseconds = DefaultInterval)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastExecutionTime).TotalMilliseconds;
            return elapsed >= intervalMilliseconds;
        }
    }

    /// <summary>
    /// 重置节流计时器
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _lastExecutionTime = DateTime.MinValue;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // 无需特殊清理
        }

        _disposed = true;
    }

    ~ThrottleService()
    {
        Dispose(false);
    }
}
