using SignalRDemo.Shared.Models;

namespace SignalRDemo.Client.Services.Abstractions;

/// <summary>
/// 消息服务接口 - 管理消息业务逻辑
/// </summary>
public interface IMessageService
{
    /// <summary>
    /// 当前房间的所有消息
    /// </summary>
    IReadOnlyList<ChatMessage> CurrentMessages { get; }
    
    /// <summary>
    /// 是否正在加载消息历史
    /// </summary>
    bool IsLoadingHistory { get; }
    
    /// <summary>
    /// 消息接收事件
    /// </summary>
    event Action<ChatMessage>? MessageReceived;
    
    /// <summary>
    /// 消息历史加载完成事件
    /// </summary>
    event Action<IReadOnlyList<ChatMessage>>? MessageHistoryLoaded;
    
    /// <summary>
    /// 发送节流状态变化事件
    /// </summary>
    event Action<bool>? ThrottledChanged;
    
    /// <summary>
    /// 初始化服务
    /// </summary>
    Task InitializeAsync();
    
    /// <summary>
    /// 加载指定房间的消息历史
    /// </summary>
    Task LoadMessageHistoryAsync(string roomId, int count = 50);
    
    /// <summary>
    /// 发送房间消息
    /// </summary>
    Task SendMessageAsync(string roomId, string message, MessageType type = MessageType.Text);
    
    /// <summary>
    /// 切换到新房间（清空当前消息并加载新消息）
    /// </summary>
    Task SwitchRoomAsync(string roomId);
    
    /// <summary>
    /// 清空当前消息
    /// </summary>
    void ClearMessages();
}
