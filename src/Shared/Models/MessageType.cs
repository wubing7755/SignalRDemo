namespace SignalRDemo.Shared.Models;

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    /// <summary>
    /// 文本消息
    /// </summary>
    Text = 0,

    /// <summary>
    /// 图片消息
    /// </summary>
    Image = 1,

    /// <summary>
    /// 表情消息
    /// </summary>
    Emoji = 2,

    /// <summary>
    /// 文件消息
    /// </summary>
    File = 3
}
