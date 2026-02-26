using SignalRDemo.Shared.Models;

namespace SignalRDemo.Infrastructure.Services;

/// <summary>
/// 房间服务接口
/// </summary>
public interface IRoomService
{
    Task<ChatRoom?> CreateRoomAsync(string name, string? description, string ownerId, bool isPublic, string? password);
    Task<ChatRoom?> GetRoomByIdAsync(string roomId);
    Task<ChatRoom?> GetRoomByNameAsync(string name);
    Task<List<ChatRoom>> GetPublicRoomsAsync();
    Task<List<ChatRoom>> GetUserRoomsAsync(string userId);
    Task<List<ChatRoom>> FindRoomsByNameAsync(string name);
    Task<bool> AddUserToRoomAsync(string userId, string roomId);
    Task<bool> RemoveUserFromRoomAsync(string userId, string roomId);
    Task<List<string>> GetRoomUserIdsAsync(string roomId);
    Task<bool> IsUserInRoomAsync(string userId, string roomId);
    Task<bool> VerifyPasswordAsync(string roomId, string password);
}
