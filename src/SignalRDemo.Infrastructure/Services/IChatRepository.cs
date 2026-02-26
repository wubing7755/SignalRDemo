using SignalRDemo.Shared.Models;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 消息仓储接口
/// </summary>
public interface IChatRepository
{
    Task SaveMessageAsync(ChatMessage message);
    Task SaveRoomMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetRecentMessagesAsync(int count = 50);
    Task<List<ChatMessage>> GetRoomMessagesAsync(string roomId, int count = 50);
}
