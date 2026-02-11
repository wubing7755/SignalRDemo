using Microsoft.AspNetCore.Components;

namespace SignalRDemo.Client.Infrastructure;

/// <summary>
/// Dialog 对话框服务
/// 提供全局对话框管理功能，支持从任何组件打开对话框
/// </summary>
public class DialogService
{
    private readonly List<DialogReference> _dialogs = new();
    private readonly NavigationManager _navigationManager;

    public DialogService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    /// <summary>
    /// 当前正在显示的对话框列表
    /// </summary>
    public IReadOnlyList<DialogReference> Dialogs => _dialogs.AsReadOnly();

    /// <summary>
    /// 对话框状态变化事件
    /// </summary>
    public event Action? OnDialogStateChanged;

    /// <summary>
    /// 异步显示对话框
    /// </summary>
    /// <typeparam name="TDialog">对话框组件类型</typeparam>
    /// <param name="title">对话框标题</param>
    /// <param name="parameters">传递给对话框的参数</param>
    /// <returns>对话框结果</returns>
    public async Task<DialogResult> ShowAsync<TDialog>(string title, Dictionary<string, object?>? parameters = null) where TDialog : IComponent
    {
        // 参数验证
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("对话框标题不能为空", nameof(title));
        }

        var dialogReference = new DialogReference
        {
            Id = Guid.NewGuid().ToString(),
            Title = title,
            ComponentType = typeof(TDialog),
            Parameters = parameters ?? new Dictionary<string, object?>(),
            TaskCompletionSource = new TaskCompletionSource<DialogResult>()
        };

        _dialogs.Add(dialogReference);
        OnDialogStateChanged?.Invoke();
        
        // 强制刷新 UI
        _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: false);

        return await dialogReference.TaskCompletionSource.Task;
    }

    /// <summary>
    /// 显示带有返回数据的对话框
    /// </summary>
    /// <typeparam name="TDialog">对话框组件类型</typeparam>
    /// <typeparam name="TResult">返回数据类型</typeparam>
    /// <param name="title">对话框标题</param>
    /// <param name="parameters">传递给对话框的参数</param>
    /// <returns>对话框结果</returns>
    public async Task<DialogResult<TResult>> ShowAsync<TDialog, TResult>(string title, Dictionary<string, object?>? parameters = null) where TDialog : IComponent
    {
        var result = await ShowAsync<TDialog>(title, parameters);
        return new DialogResult<TResult>
        {
            Data = (TResult?)result.Data,
            Cancelled = result.Cancelled
        };
    }

    /// <summary>
    /// 关闭指定的对话框
    /// </summary>
    /// <param name="dialogId">对话框ID</param>
    /// <param name="result">返回结果</param>
    public void Close(string dialogId, object? result = null)
    {
        var dialog = _dialogs.FirstOrDefault(d => d.Id == dialogId);
        if (dialog != null)
        {
            Close(dialog, result);
        }
    }

    /// <summary>
    /// 关闭对话框
    /// </summary>
    /// <param name="dialogReference">对话框引用</param>
    /// <param name="result">返回结果</param>
    public void Close(DialogReference dialogReference, object? result = null)
    {
        if (_dialogs.Remove(dialogReference))
        {
            dialogReference.TaskCompletionSource.SetResult(new DialogResult
            {
                Data = result,
                Cancelled = false
            });
            OnDialogStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// 取消并关闭对话框
    /// </summary>
    /// <param name="dialogId">对话框ID</param>
    public void Cancel(string dialogId)
    {
        var dialog = _dialogs.FirstOrDefault(d => d.Id == dialogId);
        if (dialog != null)
        {
            Cancel(dialog);
        }
    }

    /// <summary>
    /// 取消并关闭对话框
    /// </summary>
    /// <param name="dialogReference">对话框引用</param>
    public void Cancel(DialogReference dialogReference)
    {
        if (_dialogs.Remove(dialogReference))
        {
            dialogReference.TaskCompletionSource.SetResult(new DialogResult
            {
                Data = null,
                Cancelled = true
            });
            OnDialogStateChanged?.Invoke();
        }
    }

    /// <summary>
    /// 关闭所有对话框
    /// </summary>
    public void CloseAll()
    {
        foreach (var dialog in _dialogs.ToList())
        {
            dialog.TaskCompletionSource.SetResult(new DialogResult
            {
                Data = null,
                Cancelled = true
            });
        }
        _dialogs.Clear();
        OnDialogStateChanged?.Invoke();
    }
}

/// <summary>
/// 对话框引用
/// </summary>
public class DialogReference
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public Type ComponentType { get; set; } = typeof(IComponent);
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public TaskCompletionSource<DialogResult> TaskCompletionSource { get; set; } = new();
}

/// <summary>
/// 对话框结果
/// </summary>
public class DialogResult
{
    public object? Data { get; set; }
    public bool Cancelled { get; set; }
}

/// <summary>
/// 带类型的对话框结果
/// </summary>
/// <typeparam name="T"></typeparam>
public class DialogResult<T> : DialogResult
{
    public new T? Data
    {
        get => (T?)base.Data;
        set => base.Data = value;
    }
}
