using Microsoft.AspNetCore.Components;
using SignalRDemo.Client.Infrastructure;

namespace SignalRDemo.Client.Abstractions;

/// <summary>
/// Dialog 基类组件
/// 提供通用的对话框功能，简化各 Dialog 的实现
/// </summary>
public class DialogBase : ComponentBase
{
    /// <summary>
    /// Dialog 引用，用于调用 Close/Cancel
    /// </summary>
    [Parameter]
    public DialogReference? DialogReference { get; set; }

    /// <summary>
    /// 是否可以关闭对话框（用于表单验证等场景）
    /// </summary>
    protected virtual bool CanClose => true;

    /// <summary>
    /// 关闭对话框并返回结果
    /// </summary>
    /// <param name="result">返回的数据</param>
    protected void Close(object? result = null)
    {
        if (DialogReference == null || !CanClose)
        {
            return;
        }
        DialogService.Close(DialogReference, result);
    }

    /// <summary>
    /// 取消对话框
    /// </summary>
    protected void Cancel()
    {
        if (DialogReference != null)
        {
            DialogService.Cancel(DialogReference);
        }
    }

    /// <summary>
    /// 获取 DialogService 实例
    /// </summary>
    [Inject]
    protected DialogService DialogService { get; set; } = null!;
}
